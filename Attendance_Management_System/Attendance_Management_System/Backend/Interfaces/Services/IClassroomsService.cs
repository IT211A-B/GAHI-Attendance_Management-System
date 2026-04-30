using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;

namespace Attendance_Management_System.Backend.Interfaces.Services;

public interface IClassroomsService
{
    Task<ApiResponse<List<ClassroomDto>>> GetAllClassroomsAsync();
    Task<ApiResponse<ClassroomDto>> GetClassroomByIdAsync(int id);
    Task<ApiResponse<ClassroomDto>> CreateClassroomAsync(CreateClassroomRequest request);
    Task<ApiResponse<ClassroomDto>> UpdateClassroomAsync(int id, UpdateClassroomRequest request);
    Task<ApiResponse<bool>> DeleteClassroomAsync(int id);
}