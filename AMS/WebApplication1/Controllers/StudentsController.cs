using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models.Entities;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public StudentsController(AppDbContext context)
    {
        _context = context;
    }

    private string GetUserId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
    }

    // GET: api/subjects/5/students - Get students for a subject
    [HttpGet("/api/subjects/{subjectId}/students")]
    public async Task<ActionResult<IEnumerable<Student>>> GetStudentsBySubject(int subjectId)
    {
        var userId = GetUserId();

        // Verify the subject belongs to the teacher
        var subject = await _context.Subjects
            .FirstOrDefaultAsync(s => s.Id == subjectId && s.TeacherId == userId);

        if (subject == null)
        {
            return NotFound();
        }

        return await _context.Students
            .Where(s => s.SubjectId == subjectId)
            .ToListAsync();
    }

    // GET: api/students/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Student>> GetStudent(int id)
    {
        var userId = GetUserId();

        var student = await _context.Students
            .Include(s => s.Subject)
            .FirstOrDefaultAsync(s => s.Id == id && s.Subject.TeacherId == userId);

        if (student == null)
        {
            return NotFound();
        }

        return student;
    }

    // POST: api/subjects/5/students
    [HttpPost("/api/subjects/{subjectId}/students")]
    public async Task<ActionResult<Student>> CreateStudent(int subjectId, Student student)
    {
        var userId = GetUserId();

        // Verify the subject belongs to the teacher
        var subject = await _context.Subjects
            .FirstOrDefaultAsync(s => s.Id == subjectId && s.TeacherId == userId);

        if (subject == null)
        {
            return NotFound();
        }

        student.SubjectId = subjectId;
        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetStudent), new { id = student.Id }, student);
    }

    // PUT: api/students/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateStudent(int id, Student student)
    {
        var userId = GetUserId();

        if (id != student.Id)
        {
            return BadRequest();
        }

        var existingStudent = await _context.Students
            .Include(s => s.Subject)
            .FirstOrDefaultAsync(s => s.Id == id && s.Subject.TeacherId == userId);

        if (existingStudent == null)
        {
            return NotFound();
        }

        existingStudent.FullName = student.FullName;
        existingStudent.StudentNo = student.StudentNo;
        existingStudent.Contact = student.Contact;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!StudentExists(id))
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

    // DELETE: api/students/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteStudent(int id)
    {
        var userId = GetUserId();

        var student = await _context.Students
            .Include(s => s.Subject)
            .FirstOrDefaultAsync(s => s.Id == id && s.Subject.TeacherId == userId);

        if (student == null)
        {
            return NotFound();
        }

        _context.Students.Remove(student);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool StudentExists(int id)
    {
        return _context.Students.Any(s => s.Id == id);
    }
}
