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
public class SchedulesController : ControllerBase
{
    private readonly AppDbContext _context;

    public SchedulesController(AppDbContext context)
    {
        _context = context;
    }

    private string GetUserId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
    }

    // GET: api/schedules/me - Get teacher's schedules
    [HttpGet("me")]
    public async Task<ActionResult<IEnumerable<Schedule>>> GetMySchedules()
    {
        var userId = GetUserId();
        return await _context.Schedules
            .Include(s => s.Subject)
            .Include(s => s.Room)
            .Where(s => s.Subject.TeacherId == userId)
            .ToListAsync();
    }

    // GET: api/rooms/{id}/schedule - Get room schedule grid
    [HttpGet("/api/rooms/{roomId}/schedule")]
    public async Task<ActionResult<IEnumerable<Schedule>>> GetRoomSchedule(int roomId)
    {
        var userId = GetUserId();

        // Check if room exists
        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null || !room.IsActive)
        {
            return NotFound();
        }

        return await _context.Schedules
            .Include(s => s.Subject)
            .Where(s => s.RoomId == roomId)
            .ToListAsync();
    }

    // GET: api/schedules/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Schedule>> GetSchedule(int id)
    {
        var userId = GetUserId();
        var schedule = await _context.Schedules
            .Include(s => s.Subject)
            .Include(s => s.Room)
            .FirstOrDefaultAsync(s => s.Id == id && s.Subject.TeacherId == userId);

        if (schedule == null)
        {
            return NotFound();
        }

        return schedule;
    }

    // POST: api/schedules
    [HttpPost]
    public async Task<ActionResult<Schedule>> CreateSchedule(Schedule schedule)
    {
        var userId = GetUserId();

        // Verify the subject belongs to the teacher
        var subject = await _context.Subjects
            .FirstOrDefaultAsync(s => s.Id == schedule.SubjectId && s.TeacherId == userId);

        if (subject == null)
        {
            return NotFound("Subject not found or does not belong to you");
        }

        // Verify room exists
        var room = await _context.Rooms.FindAsync(schedule.RoomId);
        if (room == null || !room.IsActive)
        {
            return BadRequest("Room not found or inactive");
        }

        // Check for scheduling conflicts
        var hasConflict = await _context.Schedules
            .Include(s => s.Subject)
            .Where(s => s.RoomId == schedule.RoomId
                && s.DayOfWeek == schedule.DayOfWeek
                && s.Subject.TeacherId == userId
                && ((schedule.StartTime >= s.StartTime && schedule.StartTime < s.EndTime)
                    || (schedule.EndTime > s.StartTime && schedule.EndTime <= s.EndTime)
                    || (schedule.StartTime <= s.StartTime && schedule.EndTime >= s.EndTime)))
            .AnyAsync();

        if (hasConflict)
        {
            return Conflict("Schedule conflicts with an existing class in this room");
        }

        _context.Schedules.Add(schedule);
        await _context.SaveChangesAsync();

        // Load related data
        await _context.Entry(schedule).Reference(s => s.Subject).LoadAsync();
        await _context.Entry(schedule).Reference(s => s.Room).LoadAsync();

        return CreatedAtAction(nameof(GetSchedule), new { id = schedule.Id }, schedule);
    }

    // PUT: api/schedules/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSchedule(int id, Schedule schedule)
    {
        var userId = GetUserId();

        if (id != schedule.Id)
        {
            return BadRequest();
        }

        var existingSchedule = await _context.Schedules
            .Include(s => s.Subject)
            .FirstOrDefaultAsync(s => s.Id == id && s.Subject.TeacherId == userId);

        if (existingSchedule == null)
        {
            return NotFound();
        }

        // Verify room exists
        var room = await _context.Rooms.FindAsync(schedule.RoomId);
        if (room == null || !room.IsActive)
        {
            return BadRequest("Room not found or inactive");
        }

        // Check for scheduling conflicts (excluding current schedule)
        var hasConflict = await _context.Schedules
            .Include(s => s.Subject)
            .Where(s => s.Id != id
                && s.RoomId == schedule.RoomId
                && s.DayOfWeek == schedule.DayOfWeek
                && s.Subject.TeacherId == userId
                && ((schedule.StartTime >= s.StartTime && schedule.StartTime < s.EndTime)
                    || (schedule.EndTime > s.StartTime && schedule.EndTime <= s.EndTime)
                    || (schedule.StartTime <= s.StartTime && schedule.EndTime >= s.EndTime)))
            .AnyAsync();

        if (hasConflict)
        {
            return Conflict("Schedule conflicts with an existing class in this room");
        }

        existingSchedule.SubjectId = schedule.SubjectId;
        existingSchedule.RoomId = schedule.RoomId;
        existingSchedule.DayOfWeek = schedule.DayOfWeek;
        existingSchedule.StartTime = schedule.StartTime;
        existingSchedule.EndTime = schedule.EndTime;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ScheduleExists(id))
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

    // DELETE: api/schedules/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSchedule(int id)
    {
        var userId = GetUserId();
        var schedule = await _context.Schedules
            .Include(s => s.Subject)
            .FirstOrDefaultAsync(s => s.Id == id && s.Subject.TeacherId == userId);

        if (schedule == null)
        {
            return NotFound();
        }

        _context.Schedules.Remove(schedule);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ScheduleExists(int id)
    {
        var userId = GetUserId();
        return _context.Schedules.Include(s => s.Subject).Any(s => s.Id == id && s.Subject.TeacherId == userId);
    }
}
