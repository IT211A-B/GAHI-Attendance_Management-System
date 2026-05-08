using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;

namespace Attendance_Management_System.Backend.Interfaces.Services;

public interface IClassroomsService
{
    Task<List<ClassroomDto>> GetAllClassroomsAsync();
    Task<ClassroomDto> GetClassroomByIdAsync(int id);
    Task<ClassroomDto> CreateClassroomAsync(CreateClassroomRequest request);
    Task<ClassroomDto> UpdateClassroomAsync(int id, UpdateClassroomRequest request);
    Task DeleteClassroomAsync(int id);
}