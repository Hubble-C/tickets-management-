namespace tickets_management.Response;

public class ServiceResponse<T>
{
    public T? Data { get; set; }
    public string? Message { get; set; }
    public bool Success { get; set; }

    public static ServiceResponse<T> Ok(T data, string? message = null) =>
        new() { Data = data, Success = true, Message = message };

    public static ServiceResponse<T> Fail(string message) =>
        new() { Success = false, Message = message };
}