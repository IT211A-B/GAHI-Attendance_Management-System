using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;

namespace Attendance_Management_System.Backend.Interfaces.Services;

public interface ICoursesService
{
    Task<List<CourseDto>> GetAllCoursesAsync();
    Task<CourseDto> GetCourseByIdAsync(int id);
    Task<CourseDto> CreateCourseAsync(CreateCourseRequest request);
    Task<CourseDto> UpdateCourseAsync(int id, UpdateCourseRequest request);
    Task DeleteCourseAsync(int id);
}