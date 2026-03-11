using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models.Entities;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubjectsController : ControllerBase
{
    private readonly AppDbContext _context;

    public SubjectsController(AppDbContext context)
    {
        _context = context;
    }

    private string GetUserId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
    }

    // GET: api/subjects/me - Get teacher's subjects
    [HttpGet("me")]
    public async Task<ActionResult<IEnumerable<Subject>>> GetMySubjects()
    {
        var userId = GetUserId();
        return await _context.Subjects
            .Where(s => s.TeacherId == userId)
            .Include(s => s.Students)
            .ToListAsync();
    }

    // GET: api/subjects/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Subject>> GetSubject(int id)
    {
        var userId = GetUserId();
        var subject = await _context.Subjects
            .Include(s => s.Students)
            .Include(s => s.Schedules)
            .FirstOrDefaultAsync(s => s.Id == id && s.TeacherId == userId);

        if (subject == null)
        {
            return NotFound();
        }

        return subject;
    }

    // POST: api/subjects
    [HttpPost]
    public async Task<ActionResult<Subject>> CreateSubject(Subject subject)
    {
        var userId = GetUserId();
        subject.TeacherId = userId;

        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSubject), new { id = subject.Id }, subject);
    }

    // PUT: api/subjects/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSubject(int id, Subject subject)
    {
        var userId = GetUserId();

        if (id != subject.Id)
        {
            return BadRequest();
        }

        var existingSubject = await _context.Subjects
            .FirstOrDefaultAsync(s => s.Id == id && s.TeacherId == userId);

        if (existingSubject == null)
        {
            return NotFound();
        }

        existingSubject.Name = subject.Name;
        existingSubject.Code = subject.Code;
        existingSubject.Description = subject.Description;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SubjectExists(id))
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

    // DELETE: api/subjects/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSubject(int id)
    {
        var userId = GetUserId();
        var subject = await _context.Subjects
            .FirstOrDefaultAsync(s => s.Id == id && s.TeacherId == userId);

        if (subject == null)
        {
            return NotFound();
        }

        _context.Subjects.Remove(subject);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool SubjectExists(int id)
    {
        var userId = GetUserId();
        return _context.Subjects.Any(s => s.Id == id && s.TeacherId == userId);
    }
}
