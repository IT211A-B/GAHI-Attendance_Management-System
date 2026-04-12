using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;

namespace Attendance_Management_System.Backend.Interfaces.Services;

public interface ICoursesService
{
    Task<ApiResponse<List<CourseDto>>> GetAllCoursesAsync();
    Task<ApiResponse<CourseDto>> GetCourseByIdAsync(int id);
    Task<ApiResponse<CourseDto>> CreateCourseAsync(CreateCourseRequest request);
    Task<ApiResponse<CourseDto>> UpdateCourseAsync(int id, UpdateCourseRequest request);
    Task<ApiResponse<bool>> DeleteCourseAsync(int id);
}