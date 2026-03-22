namespace Attendance_Management_System.Backend.Exceptions;

// Base exception class for domain-specific business rule violations
// Used to differentiate application exceptions from framework exceptions
public class DomainException : Exception
{
    public DomainException()
    {
    }

    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}