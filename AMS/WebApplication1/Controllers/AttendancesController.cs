using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.Entities;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendancesController : ControllerBase
{
    private readonly AppDbContext _context;

    public AttendancesController(AppDbContext context)
    {
        _context = context;
    }

    private string GetUserId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
    }

    // GET: api/attendance?scheduleId=5&date=2024-01-01
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClassAttendance>>> GetAttendance([FromQuery] int scheduleId, [FromQuery] DateTime date)
    {
        var userId = GetUserId();

        // Verify schedule belongs to teacher
        var schedule = await _context.Schedules
            .Include(s => s.Subject)
            .FirstOrDefaultAsync(s => s.Id == scheduleId && s.Subject.TeacherId == userId);

        if (schedule == null)
        {
            return NotFound();
        }

        return await _context.ClassAttendances
            .Include(a => a.Student)
            .Where(a => a.ScheduleId == scheduleId && a.Date.Date == date.Date)
            .ToListAsync();
    }

    // GET: api/attendance/history?subjectId=5
    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<ClassAttendance>>> GetAttendanceHistory([FromQuery] int subjectId)
    {
        var userId = GetUserId();

        // Verify subject belongs to teacher
        var subject = await _context.Subjects
            .FirstOrDefaultAsync(s => s.Id == subjectId && s.TeacherId == userId);

        if (subject == null)
        {
            return NotFound();
        }

        return await _context.ClassAttendances
            .Include(a => a.Student)
            .Include(a => a.Schedule)
            .Where(a => a.Schedule.SubjectId == subjectId)
            .OrderByDescending(a => a.Date)
            .ToListAsync();
    }

    // POST: api/attendance/bulk
    [HttpPost("bulk")]
    public async Task<ActionResult<IEnumerable<ClassAttendance>>> BulkCreateAttendance(List<ClassAttendance> attendances)
    {
        var userId = GetUserId();

        if (attendances == null || attendances.Count == 0)
        {
            return BadRequest("No attendance records provided");
        }

        var scheduleId = attendances[0].ScheduleId;

        // Verify schedule belongs to teacher
        var schedule = await _context.Schedules
            .Include(s => s.Subject)
            .FirstOrDefaultAsync(s => s.Id == scheduleId && s.Subject.TeacherId == userId);

        if (schedule == null)
        {
            return NotFound("Schedule not found or does not belong to you");
        }

        var date = attendances[0].Date.Date;

        // Get existing attendance records for this schedule and date
        var existingRecords = await _context.ClassAttendances
            .Where(a => a.ScheduleId == scheduleId && a.Date.Date == date.Date)
            .ToListAsync();

        // Remove existing records
        _context.ClassAttendances.RemoveRange(existingRecords);

        // Add new records
        foreach (var attendance in attendances)
        {
            attendance.ScheduleId = scheduleId;
            attendance.Date = date;
            attendance.RecordedBy = userId;
            _context.ClassAttendances.Add(attendance);
        }

        await _context.SaveChangesAsync();

        // Return updated records
        var result = await _context.ClassAttendances
            .Include(a => a.Student)
            .Where(a => a.ScheduleId == scheduleId && a.Date.Date == date.Date)
            .ToListAsync();

        return Ok(result);
    }

    // PUT: api/attendance/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAttendance(int id, ClassAttendance attendance)
    {
        var userId = GetUserId();

        if (id != attendance.Id)
        {
            return BadRequest();
        }

        var existingAttendance = await _context.ClassAttendances
            .Include(a => a.Schedule)
            .ThenInclude(s => s.Subject)
            .FirstOrDefaultAsync(a => a.Id == id && a.Schedule.Subject.TeacherId == userId);

        if (existingAttendance == null)
        {
            return NotFound();
        }

        existingAttendance.Status = attendance.Status;
        existingAttendance.Remarks = attendance.Remarks;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!AttendanceExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // GET: api/attendance/schedule/5/date/2024-01-01/students
    [HttpGet("schedule/{scheduleId}/date/{date}/students")]
    public async Task<ActionResult<IEnumerable<Student>>> GetStudentsForAttendance(int scheduleId, DateTime date)
    {
        var userId = GetUserId();

        // Verify schedule belongs to teacher
        var schedule = await _context.Schedules
            .Include(s => s.Subject)
            .FirstOrDefaultAsync(s => s.Id == scheduleId && s.Subject.TeacherId == userId);

        if (schedule == null)
        {
            return NotFound();
        }

        // Get all students in this subject
        var students = await _context.Students
            .Where(s => s.SubjectId == schedule.SubjectId)
            .ToListAsync();

        // Get existing attendance records
        var attendanceRecords = await _context.ClassAttendances
            .Where(a => a.ScheduleId == scheduleId && a.Date.Date == date.Date)
            .ToListAsync();

        // Mark students with attendance status
        var result = students.Select(s =>
        {
            var attendance = attendanceRecords.FirstOrDefault(a => a.StudentId == s.Id);
            if (attendance != null)
            {
                s.Attendances = new List<ClassAttendance> { attendance };
            }
            return s;
        }).ToList();

        return Ok(result);
    }

    private bool AttendanceExists(int id)
    {
        return _context.ClassAttendances.Any(a => a.Id == id);
    }
}
