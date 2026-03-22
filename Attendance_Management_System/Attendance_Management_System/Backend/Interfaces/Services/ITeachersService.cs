using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;

namespace Attendance_Management_System.Backend.Interfaces.Services;

public interface ITeachersService
{
    Task<ApiResponse<List<TeacherDto>>> GetAllTeachersAsync();
    Task<ApiResponse<TeacherDto>> GetTeacherByIdAsync(int id);
    Task<ApiResponse<TeacherDto>> UpdateTeacherAsync(int id, UpdateTeacherRequest request);
    Task<ApiResponse<bool>> DeactivateTeacherAsync(int id);
    Task<ApiResponse<bool>> ActivateTeacherAsync(int id);
}