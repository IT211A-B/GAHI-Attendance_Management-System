namespace Attendance_Management_System.Backend.Enums;

// Defines the possible statuses for a student enrollment
// Used to track the approval workflow for section enrollments
public enum EnrollmentStatus
{
    // Enrollment submitted, awaiting admin review
    Pending,

    // Enrollment approved - student can attend classes
    Approved,

    // Enrollment rejected - see rejection reason for details
    Rejected
}
