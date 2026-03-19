namespace Donbosco_Attendance_Management_System.DTOs.Responses;

// response model for list endpoints with pagination support
public class UserListResponse
{
    public List<UserProfileResponse> Items { get; set; } = new();
    public int Total { get; set; }
}

// generic list response model for any resource type
public class ListResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
}

// response model for a single classroom
public class ClassroomResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// response model for a single section
public class SectionResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}