using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

public abstract class AppControllerBase : Controller
{
    protected static async Task<ServiceCallResult<T>> ExecuteServiceCallAsync<T>(Func<Task<T>> serviceCall)
    {
        try
        {
            var data = await serviceCall();
            return ServiceCallResult<T>.FromSuccess(data);
        }
        catch (Exception ex)
        {
            return ServiceCallResult<T>.FromError(ex.Message);
        }
    }

    protected static async Task<ServiceCallResult<bool>> ExecuteServiceCallAsync(Func<Task> serviceCall)
    {
        try
        {
            await serviceCall();
            return ServiceCallResult<bool>.FromSuccess(true);
        }
        catch (Exception ex)
        {
            return ServiceCallResult<bool>.FromError(ex.Message);
        }
    }
}

public sealed class ServiceCallResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public ServiceCallError? Error { get; init; }

    public static ServiceCallResult<T> FromSuccess(T data) => new()
    {
        Success = true,
        Data = data
    };

    public static ServiceCallResult<T> FromError(string message) => new()
    {
        Success = false,
        Error = new ServiceCallError { Message = message }
    };
}

public sealed class ServiceCallError
{
    public string Message { get; init; } = string.Empty;
}