namespace Attendance_Management_System.Backend.Configuration;

public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string Host { get; set; } = "smtp.gmail.com";

    public int Port { get; set; } = 587;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = "Don Bosco Attendance";

    public bool UseSsl { get; set; } = false;

    public string? PublicBaseUrl { get; set; }
}
