using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.Students;
using SystemManagementSystem.Services.Interfaces;

namespace SystemManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Registrar")]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;

    public StudentsController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<StudentResponse>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] Guid? sectionId = null, [FromQuery] string? search = null)
    {
        var result = await _studentService.GetAllAsync(page, pageSize, sectionId, search);
        return Ok(ApiResponse<PagedResult<StudentResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<StudentResponse>>> GetById(Guid id)
    {
        var result = await _studentService.GetByIdAsync(id);
        return Ok(ApiResponse<StudentResponse>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<StudentResponse>>> Create([FromBody] CreateStudentRequest request)
    {
        var result = await _studentService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<StudentResponse>.Ok(result, "Student created."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<StudentResponse>>> Update(Guid id, [FromBody] UpdateStudentRequest request)
    {
        var result = await _studentService.UpdateAsync(id, request);
        return Ok(ApiResponse<StudentResponse>.Ok(result, "Student updated."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        await _studentService.DeleteAsync(id);
        return Ok(ApiResponse.Ok("Student deleted."));
    }

    [HttpPost("{id:guid}/regenerate-qr")]
    public async Task<ActionResult<ApiResponse<StudentResponse>>> RegenerateQrCode(Guid id)
    {
        var result = await _studentService.RegenerateQrCodeAsync(id);
        return Ok(ApiResponse<StudentResponse>.Ok(result, "QR code regenerated."));
    }
}
