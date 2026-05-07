using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;

namespace Attendance_Management_System.Backend.Interfaces.Services;

public interface IAttendanceQrService
{
    Task<List<AttendanceQrSectionSuggestionDto>> SearchSectionsAsync(int userId, string role, string? query, int take);

    Task<List<AttendanceQrSubjectSuggestionDto>> SearchSubjectsAsync(int userId, string role, int sectionId, string? query, int take);

    Task<List<AttendanceQrPeriodSuggestionDto>> SearchPeriodsAsync(int userId, string role, int sectionId, int subjectId, string? query, int take);

    Task<AttendanceQrSessionDto> CreateSessionAsync(int userId, string role, CreateAttendanceQrSessionRequest request);

    Task<AttendanceQrSessionDto> RefreshSessionAsync(int userId, string role, string sessionId);

    Task CloseSessionAsync(int userId, string role, string sessionId);

    Task<AttendanceQrLiveFeedDto> GetLiveFeedAsync(int userId, string role, string sessionId);

    Task<AttendanceQrCheckinResultDto> SubmitCheckinAsync(int userId, string role, SubmitAttendanceQrCheckinRequest request);
}

