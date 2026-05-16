using System.Text.Json;
using Attendance_Management_System.Backend.Configuration;
using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Enums;
using Attendance_Management_System.Backend.Helpers;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Attendance_Management_System.Backend.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Attendance_Management_System.Backend.Services;

// Manages student enrollment lifecycle: submission, approval, rejection, and section reassignment.
// Handles capacity warnings and notifies students of status changes via email and in-app notifications.
public class EnrollmentService : IEnrollmentService
{
    private static readonly string EnrollmentPendingStatus = EnrollmentStatus.Pending.ToStorageValue();
    private static readonly string EnrollmentApprovedStatus = EnrollmentStatus.Approved.ToStorageValue();
    private static readonly string EnrollmentRejectedStatus = EnrollmentStatus.Rejected.ToStorageValue();

    private readonly AppDbContext _context;
    private readonly EnrollmentSettings _enrollmentSettings;
    private readonly ISectionAllocationService _sectionAllocationService;
    private readonly INotificationService _notificationService;
    private readonly IAccountEmailService _accountEmailService;
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(
        AppDbContext context,
        IOptions<EnrollmentSettings> enrollmentSettings,
        ISectionAllocationService sectionAllocationService,
        INotificationService notificationService,
        IAccountEmailService accountEmailService,
        ILogger<EnrollmentService> logger)
    {
        _context = context;
        _enrollmentSettings = enrollmentSettings.Value?.IsValid() == true
            ? enrollmentSettings.Value
            : EnrollmentSettings.Default;
        _sectionAllocationService = sectionAllocationService;
        _notificationService = notificationService;
        _accountEmailService = accountEmailService;
        _logger = logger;
    }

    // Gets a paginated list of enrollments with "pending" status
    // Pre-loads teacher data to avoid N+1 query problems
    public async Task<EnrollmentListDto> GetPendingEnrollmentsAsync(int? academicYearId, int page, int pageSize)
    {
        // Build query for pending enrollments with related data
        var query = _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Section)
            .Include(e => e.AcademicYear)
            .Include(e => e.Processor)
            .Where(e => e.Status == EnrollmentPendingStatus);

        // Filter by academic year if provided
        if (academicYearId.HasValue)
        {
            query = query.Where(e => e.AcademicYearId == academicYearId.Value);
        }

        var totalCount = await query.CountAsync();

        // Apply pagination - get records for the current page
        var enrollments = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Pre-load teacher data for processors to avoid N+1 query problem
        var processorUserIds = enrollments
            .Where(e => e.ProcessedBy.HasValue)
            .Select(e => e.ProcessedBy!.Value)
            .Distinct()
            .ToHashSet();

        var teacherNames = await _context.Teachers
            .Where(t => processorUserIds.Contains(t.UserId))
            .ToDictionaryAsync(t => t.UserId, t => $"{t.FirstName} {t.LastName}");

        // Get counts for each status to display in the UI
        var pendingCount = await _context.Enrollments.CountAsync(e => e.Status == EnrollmentPendingStatus);
        var approvedCount = await _context.Enrollments.CountAsync(e => e.Status == EnrollmentApprovedStatus);
        var rejectedCount = await _context.Enrollments.CountAsync(e => e.Status == EnrollmentRejectedStatus);

        return new EnrollmentListDto
        {
            TotalCount = totalCount,
            PendingCount = pendingCount,
            ApprovedCount = approvedCount,
            RejectedCount = rejectedCount,
            Enrollments = enrollments.Select(e => MapToDto(e, teacherNames)).ToList()
        };
    }

    // Gets a paginated list of all enrollments with optional filtering by status and academic year
    // Pre-loads teacher data to avoid N+1 query problems
    public async Task<EnrollmentListDto> GetAllEnrollmentsAsync(string? status, int? academicYearId, int page, int pageSize)
    {
        // Build query with related data
        var query = _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Section)
            .Include(e => e.AcademicYear)
            .Include(e => e.Processor)
            .AsQueryable();

        // Filter by status if provided
        if (!string.IsNullOrEmpty(status))
        {
            if (EnumStorage.TryParseEnrollmentStatus(status, out var requestedStatus))
            {
                var normalizedStatus = requestedStatus.ToStorageValue();
                query = query.Where(e => e.Status == normalizedStatus);
            }
            else
            {
                // Unknown status filters intentionally return no rows instead of throwing.
                query = query.Where(_ => false);
            }
        }

        // Filter by academic year if provided
        if (academicYearId.HasValue)
        {
            query = query.Where(e => e.AcademicYearId == academicYearId.Value);
        }

        var totalCount = await query.CountAsync();

        // Apply pagination - get records for the current page
        var enrollments = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Pre-load teacher data for processors to avoid N+1 query problem
        var processorUserIds = enrollments
            .Where(e => e.ProcessedBy.HasValue)
            .Select(e => e.ProcessedBy!.Value)
            .Distinct()
            .ToHashSet();

        var teacherNames = await _context.Teachers
            .Where(t => processorUserIds.Contains(t.UserId))
            .ToDictionaryAsync(t => t.UserId, t => $"{t.FirstName} {t.LastName}");

        // Get counts for each status to display in the UI
        var pendingCount = await _context.Enrollments.CountAsync(e => e.Status == EnrollmentPendingStatus);
        var approvedCount = await _context.Enrollments.CountAsync(e => e.Status == EnrollmentApprovedStatus);
        var rejectedCount = await _context.Enrollments.CountAsync(e => e.Status == EnrollmentRejectedStatus);

        return new EnrollmentListDto
        {
            TotalCount = totalCount,
            PendingCount = pendingCount,
            ApprovedCount = approvedCount,
            RejectedCount = rejectedCount,
            Enrollments = enrollments.Select(e => MapToDto(e, teacherNames)).ToList()
        };
    }

    // Updates the status of an enrollment (approve or reject) - admin only operation
    // Validates business rules before updating and assigns student to section if approved
    public async Task<EnrollmentDto> UpdateEnrollmentStatusAsync(int enrollmentId, UpdateEnrollmentStatusRequest request, int adminId)
    {
        // Get enrollment with all related data
        var enrollment = await _context.Enrollments
            .Include(e => e.Student)
                .ThenInclude(student => student!.User)
            .Include(e => e.Section)
            .Include(e => e.AcademicYear)
            .Include(e => e.Processor)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId);

        if (enrollment == null)
        {
            throw new KeyNotFoundException("Enrollment not found.");
        }

        // Only pending enrollments can be processed
        if (enrollment.Status != EnrollmentPendingStatus)
        {
            throw new InvalidOperationException("This enrollment has already been processed.");
        }

        // Validate and normalize status from the existing enum values.
        if (!EnumStorage.TryParseEnrollmentStatus(request.Status, out var requestedStatus)
            || (requestedStatus != EnrollmentStatus.Approved && requestedStatus != EnrollmentStatus.Rejected))
        {
            throw new InvalidOperationException("Status must be 'approved' or 'rejected'.");
        }

        var status = requestedStatus.ToStorageValue();

        // Require rejection reason when rejecting
        if (status == EnrollmentRejectedStatus && string.IsNullOrWhiteSpace(request.RejectionReason))
        {
            throw new InvalidOperationException("Rejection reason is required when rejecting an enrollment.");
        }

        // Check for duplicate approved enrollment for same student/section/year
        if (status == EnrollmentApprovedStatus)
        {
            var existingApproved = await _context.Enrollments
                .AnyAsync(e => e.StudentId == enrollment.StudentId
                    && e.SectionId == enrollment.SectionId
                    && e.AcademicYearId == enrollment.AcademicYearId
                    && e.Status == EnrollmentApprovedStatus
                    && e.Id != enrollmentId);

            if (existingApproved)
            {
                throw new InvalidOperationException("Student already has an approved enrollment for this section and academic year.");
            }
        }

        // Check capacity and generate warnings if approving
        EnrollmentWarning? warning = null;
        if (status == EnrollmentApprovedStatus)
        {
            var currentCount = await _context.Enrollments
                .CountAsync(e => e.SectionId == enrollment.SectionId && e.Status == EnrollmentApprovedStatus);

            if (currentCount >= _enrollmentSettings.OverCapacityLimit)
            {
                throw new InvalidOperationException($"Section has reached the over-capacity limit of {_enrollmentSettings.OverCapacityLimit} students.");
            }

            // Generate warning if at or above warning threshold
            if (currentCount >= _enrollmentSettings.WarningThreshold)
            {
                warning = EnrollmentWarning.OverCapacity(
                    enrollment.SectionId,
                    currentCount + 1,
                    _enrollmentSettings.WarningThreshold,
                    _enrollmentSettings.OverCapacityLimit);

                enrollment.HasWarning = true;
                enrollment.WarningMessage = warning.Message;
            }
        }

        // Update enrollment status and record who processed it
        enrollment.Status = status;
        enrollment.ProcessedAt = DateTimeOffset.UtcNow;
        enrollment.ProcessedBy = adminId;
        enrollment.RejectionReason = request.RejectionReason;

        // Ensure reviewed students can still access their existing account.
        if (enrollment.Student?.User != null)
        {
            enrollment.Student.User.IsActive = true;
        }

        // If approved, assign the student to the section.
        if (status == EnrollmentApprovedStatus)
        {
            if (enrollment.Student != null)
            {
                enrollment.Student.SectionId = enrollment.SectionId;
            }
        }

        await _context.SaveChangesAsync();

        await TryCreateEnrollmentStatusNotificationAsync(enrollment, status, request.RejectionReason);
        await TrySendEnrollmentStatusEmailAsync(enrollment, status, request.RejectionReason);

        // Get processor name for the response
        var teacher = await _context.Teachers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == adminId);

        string? processorName = teacher != null
            ? $"{teacher.FirstName} {teacher.LastName}"
            : enrollment.Processor?.Email;

        var dto = MapToDto(enrollment, new Dictionary<int, string>());
        dto.ProcessorName = processorName;

        return dto;
    }

    // Gets detailed information about a specific enrollment by its ID
    // Returns null if the enrollment is not found
    public async Task<EnrollmentDto?> GetEnrollmentByIdAsync(int enrollmentId)
    {
        // Get enrollment with all related data
        var enrollment = await _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Section)
            .Include(e => e.AcademicYear)
            .Include(e => e.Processor)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId);

        if (enrollment == null)
        {
            return null;
        }

        // Pre-load teacher data for the processor if the enrollment was processed
        var teacherNames = new Dictionary<int, string>();
        if (enrollment.ProcessedBy.HasValue)
        {
            var teacher = await _context.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == enrollment.ProcessedBy.Value);

            if (teacher != null)
            {
                teacherNames[teacher.UserId] = $"{teacher.FirstName} {teacher.LastName}";
            }
        }

        return MapToDto(enrollment, teacherNames);
    }

    // Student self-enrollment - finds matching sections by course and year level and auto-assigns
    public async Task<EnrollmentResultDto> CreateEnrollmentAsync(CreateEnrollmentRequest request, int studentUserId)
    {
        // Get the student record for the user
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.UserId == studentUserId);

        if (student == null)
        {
            throw new KeyNotFoundException("Student record not found for the current user.");
        }

        // Validate course exists
        var course = await _context.Courses.FindAsync(request.CourseId);
        if (course == null)
        {
            throw new KeyNotFoundException("Course not found.");
        }

        if (!EducationLevelPolicy.IsYearLevelAllowed(course.EducationLevel, request.YearLevel))
        {
            var allowedRange = EducationLevelPolicy.GetAllowedYearRange(course.EducationLevel);
            throw new InvalidOperationException($"Year level {request.YearLevel} is not valid for {EducationLevelPolicy.ToDisplayLabel(course.EducationLevel)}. Allowed range is {allowedRange.MinYearLevel}-{allowedRange.MaxYearLevel}.");
        }

        // Validate academic year exists
        var academicYear = await _context.AcademicYears.FindAsync(request.AcademicYearId);
        if (academicYear == null)
        {
            throw new KeyNotFoundException("Academic year not found.");
        }

        // Check for existing enrollment for this course and academic year
        var existingEnrollment = await _context.Enrollments
            .AnyAsync(e => e.StudentId == student.Id
                && e.Section != null
                && e.Section.CourseId == request.CourseId
                && e.AcademicYearId == request.AcademicYearId
                && (e.Status == EnrollmentApprovedStatus || e.Status == EnrollmentPendingStatus));

        if (existingEnrollment)
        {
            throw new InvalidOperationException("You already have an enrollment for this course and academic year.");
        }

        // Resolve section through centralized allocation policy.
        var section = await _sectionAllocationService
            .AllocateSectionAsync(request.CourseId, request.AcademicYearId, request.YearLevel);

        // If still no section available, return error
        if (section == null)
        {
            throw new InvalidOperationException("No available sections found for your course and year level. Please contact an administrator.");
        }

        if (section.YearLevel != request.YearLevel)
        {
            throw new InvalidOperationException("No available section matches the selected year level for this program.");
        }

        // Get current enrollment count for the selected section
        var currentCount = await _context.Enrollments
            .CountAsync(e => e.SectionId == section.Id && e.Status == EnrollmentApprovedStatus);

        // Create enrollment with warning if needed
        var enrollment = new Enrollment
        {
            StudentId = student.Id,
            SectionId = section.Id,
            AcademicYearId = request.AcademicYearId,
            Status = EnrollmentPendingStatus,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Add warning if section is at or above warning threshold
        var warnings = new List<EnrollmentWarning>();
        if (currentCount >= _enrollmentSettings.WarningThreshold)
        {
            enrollment.HasWarning = true;
            enrollment.WarningMessage = $"Section has {currentCount} students (warning threshold: {_enrollmentSettings.WarningThreshold}).";
            warnings.Add(EnrollmentWarning.OverCapacity(
                section.Id,
                currentCount + 1,
                _enrollmentSettings.WarningThreshold,
                _enrollmentSettings.OverCapacityLimit));
        }

        _context.Enrollments.Add(enrollment);

        // Persist chosen level so the next enrollment cycle starts from latest profile level.
        student.YearLevel = request.YearLevel;
        await _context.SaveChangesAsync();

        // Build response
        var enrollmentDto = await GetEnrollmentByIdAsync(enrollment.Id);

        return new EnrollmentResultDto
        {
            Success = true,
            Enrollment = enrollmentDto ?? throw new InvalidOperationException("Failed to retrieve created enrollment."),
            Warnings = warnings,
            Message = "Enrollment request submitted successfully. Waiting for admin approval."
        };
    }

    // Admin reassigns student to different section with capacity checks
    public async Task<EnrollmentDto> ReassignSectionAsync(int enrollmentId, ReassignSectionRequest request, int adminId)
    {
        // Get the enrollment
        var enrollment = await _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Section)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId);

        if (enrollment == null)
        {
            throw new KeyNotFoundException("Enrollment not found.");
        }

        // Validate the new section exists
        var newSection = await _context.Sections
            .FirstOrDefaultAsync(s => s.Id == request.NewSectionId);

        if (newSection == null)
        {
            throw new KeyNotFoundException("Target section not found.");
        }

        // Ensure reassignment stays within the student's assigned course.
        if (enrollment.Student == null || enrollment.Student.CourseId <= 0)
        {
            throw new InvalidOperationException("Student course is not set. Unable to validate reassignment.");
        }

        if (newSection.CourseId != enrollment.Student.CourseId)
        {
            throw new InvalidOperationException("Target section must match the student's course.");
        }

        // Check if this is the same section
        if (newSection.Id == enrollment.SectionId)
        {
            throw new InvalidOperationException("Student is already assigned to this section. Please choose a different target section.");
        }

        // Check capacity on new section
        var currentCount = await _context.Enrollments
            .CountAsync(e => e.SectionId == request.NewSectionId && e.Status == EnrollmentApprovedStatus);

        if (currentCount >= _enrollmentSettings.OverCapacityLimit)
        {
            throw new InvalidOperationException($"Target section has reached the over-capacity limit of {_enrollmentSettings.OverCapacityLimit} students.");
        }

        // Update the enrollment
        var oldSectionId = enrollment.SectionId;
        enrollment.SectionId = request.NewSectionId;

        // Add warning if new section is at or above warning threshold
        if (currentCount >= _enrollmentSettings.WarningThreshold)
        {
            enrollment.HasWarning = true;
            enrollment.WarningMessage = $"Reassigned to section with {currentCount} students (warning threshold: {_enrollmentSettings.WarningThreshold}). Reason: {request.Reason ?? "No reason provided"}";
        }
        else
        {
            enrollment.HasWarning = false;
            enrollment.WarningMessage = null;
        }

        // If the enrollment was approved, update the student's section
        if (enrollment.Status == EnrollmentApprovedStatus && enrollment.Student != null)
        {
            enrollment.Student.SectionId = request.NewSectionId;
        }

        await _context.SaveChangesAsync();

        // Return updated enrollment
        var updatedDto = await GetEnrollmentByIdAsync(enrollment.Id);
        return updatedDto ?? throw new InvalidOperationException("Failed to retrieve updated enrollment.");
    }

    // Returns current enrollment count and capacity status for a section
    public async Task<SectionCapacityDto?> GetSectionCapacityAsync(int sectionId)
    {
        var section = await _context.Sections
            .Include(s => s.Course)
            .Include(s => s.AcademicYear)
            .FirstOrDefaultAsync(s => s.Id == sectionId);

        if (section == null)
        {
            return null;
        }

        var currentCount = await _context.Enrollments
            .CountAsync(e => e.SectionId == sectionId && e.Status == EnrollmentApprovedStatus);

        var status = CalculateCapacityStatus(currentCount);
        var availableSlots = Math.Max(0, _enrollmentSettings.OverCapacityLimit - currentCount);

        return new SectionCapacityDto
        {
            SectionId = sectionId,
            SectionName = section.Name,
            YearLevel = section.YearLevel,
            CurrentEnrollment = currentCount,
            WarningThreshold = _enrollmentSettings.WarningThreshold,
            OverCapacityLimit = _enrollmentSettings.OverCapacityLimit,
            Status = status,
            AvailableSlots = availableSlots
        };
    }

    // Gets sections matching student's course and year level with capacity info
    public async Task<List<SectionCapacityDto>> GetAvailableSectionsForStudentAsync(int courseId, int yearLevel, int academicYearId)
    {
        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(selectedCourse => selectedCourse.Id == courseId);

        if (course == null || !EducationLevelPolicy.IsYearLevelAllowed(course.EducationLevel, yearLevel))
        {
            return [];
        }

        var sections = await _context.Sections
            .Include(s => s.Course)
            .Include(s => s.AcademicYear)
            .Where(s => s.CourseId == courseId && s.YearLevel == yearLevel && s.AcademicYearId == academicYearId)
            .ToListAsync();

        var result = new List<SectionCapacityDto>();
        foreach (var section in sections)
        {
            var capacity = await GetSectionCapacityAsync(section.Id);
            if (capacity != null)
            {
                result.Add(capacity);
            }
        }

        return result;
    }

    // Calculates the capacity status based on enrollment count
    private SectionCapacityStatus CalculateCapacityStatus(int currentCount)
    {
        if (currentCount >= _enrollmentSettings.OverCapacityLimit)
        {
            return SectionCapacityStatus.OverCapacity;
        }
        else if (currentCount >= _enrollmentSettings.WarningThreshold)
        {
            return SectionCapacityStatus.AtWarning;
        }
        return SectionCapacityStatus.Available;
    }

    // Maps an Enrollment entity to an EnrollmentDto using pre-loaded teacher data
    // This method does NOT perform any database queries to avoid N+1 problems
    private EnrollmentDto MapToDto(Enrollment enrollment, Dictionary<int, string> teacherNames)
    {
        string? processorName = null;

        // Resolve processor name from pre-loaded dictionary or fallback to email
        if (enrollment.ProcessedBy.HasValue)
        {
            // Try to get from pre-loaded dictionary first
            if (teacherNames.TryGetValue(enrollment.ProcessedBy.Value, out var name))
            {
                processorName = name;
            }
            else if (enrollment.Processor != null)
            {
                // Fallback to email if teacher record not found
                processorName = enrollment.Processor.Email;
            }
        }

        return new EnrollmentDto
        {
            Id = enrollment.Id,
            StudentId = enrollment.StudentId,
            StudentNumber = enrollment.Student?.StudentNumber,
            StudentName = enrollment.Student != null
                ? $"{enrollment.Student.FirstName} {enrollment.Student.LastName}"
                : null,
            StudentCourseId = enrollment.Student?.CourseId,
            SectionId = enrollment.SectionId,
            SectionName = enrollment.Section?.Name,
            AcademicYearId = enrollment.AcademicYearId,
            AcademicYearLabel = enrollment.AcademicYear?.YearLabel,
            Status = enrollment.Status,
            CreatedAt = enrollment.CreatedAt,
            ProcessedAt = enrollment.ProcessedAt,
            ProcessedBy = enrollment.ProcessedBy,
            ProcessorName = processorName,
            RejectionReason = enrollment.RejectionReason,
            HasWarning = enrollment.HasWarning,
            WarningMessage = enrollment.WarningMessage
        };
    }

    private static string BuildStudentDisplayName(string? firstName, string? middleName, string? lastName)
    {
        var fullName = string.Join(" ", new[] { firstName, middleName, lastName }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim()));

        return string.IsNullOrWhiteSpace(fullName) ? "Student" : fullName;
    }

    private async Task TryCreateEnrollmentStatusNotificationAsync(Enrollment enrollment, string status, string? rejectionReason)
    {
        if (enrollment.Student?.UserId is not int recipientUserId || recipientUserId <= 0)
        {
            return;
        }

        var studentName = BuildStudentDisplayName(enrollment.Student.FirstName, enrollment.Student.MiddleName, enrollment.Student.LastName);
        var title = status == EnrollmentApprovedStatus ? "Enrollment Approved" : "Enrollment Rejected";

        var message = status == EnrollmentApprovedStatus
            ? $"Your enrollment has been approved, {studentName}."
            : string.IsNullOrWhiteSpace(rejectionReason)
                ? $"Your enrollment has been rejected, {studentName}."
                : $"Your enrollment has been rejected: {rejectionReason.Trim()}";

        var payloadJson = JsonSerializer.Serialize(new
        {
            EnrollmentId = enrollment.Id,
            Status = status,
            SectionId = enrollment.SectionId,
            AcademicYearId = enrollment.AcademicYearId
        });

        try
        {
            await _notificationService.CreateAsync(
                recipientUserId,
                NotificationTypes.Enrollment,
                title,
                message,
                NotificationLinks.Enrollments,
                payloadJson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create enrollment notification for enrollment {EnrollmentId}.", enrollment.Id);
        }
    }

    private async Task TrySendEnrollmentStatusEmailAsync(Enrollment enrollment, string status, string? rejectionReason)
    {
        if (enrollment.Student?.User?.Email is not string recipientEmail || string.IsNullOrWhiteSpace(recipientEmail))
        {
            return;
        }

        var studentName = BuildStudentDisplayName(enrollment.Student.FirstName, enrollment.Student.MiddleName, enrollment.Student.LastName);

        try
        {
            await _accountEmailService.SendEnrollmentStatusUpdateAsync(
                recipientEmail,
                studentName,
                status,
                rejectionReason);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send enrollment status email for enrollment {EnrollmentId}.", enrollment.Id);
        }
    }
}


