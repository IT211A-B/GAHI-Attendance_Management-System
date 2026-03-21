namespace Attendance_Management_System.Backend.Validators;

public interface IAppValidator<in T>
{
    IEnumerable<string> Validate(T instance);
}
