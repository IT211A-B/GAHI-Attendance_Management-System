using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;

namespace Attendance_Management_System.Backend.Interfaces.Services;

public interface IAttendanceQrService
{
    Task<ApiResponse<List<AttendanceQrSectionSuggestionDto>>> SearchSectionsAsync(int userId, string role, string? query, int take);

    Task<ApiResponse<List<AttendanceQrSubjectSuggestionDto>>> SearchSubjectsAsync(int userId, string role, int sectionId, string? query, int take);

    Task<ApiResponse<List<AttendanceQrPeriodSuggestionDto>>> SearchPeriodsAsync(int userId, string role, int sectionId, int subjectId, string? query, int take);

    Task<ApiResponse<AttendanceQrSessionDto>> CreateSessionAsync(int userId, string role, CreateAttendanceQrSessionRequest request);

    Task<ApiResponse<AttendanceQrSessionDto>> RefreshSessionAsync(int userId, string role, string sessionId);

    Task<ApiResponse<bool>> CloseSessionAsync(int userId, string role, string sessionId);

    Task<ApiResponse<AttendanceQrLiveFeedDto>> GetLiveFeedAsync(int userId, string role, string sessionId);

    Task<ApiResponse<AttendanceQrCheckinResultDto>> SubmitCheckinAsync(int userId, string role, SubmitAttendanceQrCheckinRequest request);
}
