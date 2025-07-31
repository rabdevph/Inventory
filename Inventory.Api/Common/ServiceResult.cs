namespace Inventory.Api.Common;

public class ServiceResult<T>
{
    public bool Success { get; set; }
    public int ResponseStatusCode { get; set; }
    public string? ErrorTitle { get; set; }
    public string? ErrorMessage { get; set; }
    public T? Data { get; set; }

    public static ServiceResult<T> Ok(T data) =>
        new() { Success = true, Data = data, ResponseStatusCode = 200 };

    public static ServiceResult<T> Ok(T data, int statusCode) =>
        new() { Success = true, Data = data, ResponseStatusCode = statusCode };

    public static ServiceResult<T> Created(T data) =>
        new() { Success = true, Data = data, ResponseStatusCode = 201 };

    public static ServiceResult<T> NoContent() =>
        new() { Success = true, ResponseStatusCode = 204 };

    public static ServiceResult<T> Fail(int responseStatusCode, string errorTitle, string errorMessage) =>
        new() { Success = false, ResponseStatusCode = responseStatusCode, ErrorTitle = errorTitle, ErrorMessage = errorMessage };

    public static ServiceResult<T> NotFound(string message = "Resource not found") =>
        Fail(404, "Not Found", message);

    public static ServiceResult<T> BadRequest(string message) =>
        Fail(400, "Bad Request", message);

    public static ServiceResult<T> ValidationError(string message) =>
        Fail(422, "Validation Error", message);

    public static ServiceResult<T> Conflict(string message) =>
        Fail(409, "Conflict", message);

    public static ServiceResult<T> InternalError(string message = "An internal server error occurred") =>
        Fail(500, "Internal Server Error", message);
}

public class ServiceResult
{
    public bool Success { get; set; }
    public int ResponseStatusCode { get; set; }
    public string? ErrorTitle { get; set; }
    public string? ErrorMessage { get; set; }

    public static ServiceResult Ok() =>
        new() { Success = true, ResponseStatusCode = 200 };

    public static ServiceResult Ok(int statusCode) =>
        new() { Success = true, ResponseStatusCode = statusCode };

    public static ServiceResult Created() =>
        new() { Success = true, ResponseStatusCode = 201 };

    public static ServiceResult NoContent() =>
        new() { Success = true, ResponseStatusCode = 204 };

    public static ServiceResult Fail(int responseStatusCode, string errorTitle, string errorMessage) =>
        new() { Success = false, ResponseStatusCode = responseStatusCode, ErrorTitle = errorTitle, ErrorMessage = errorMessage };

    public static ServiceResult NotFound(string message = "Resource not found") =>
        Fail(404, "Not Found", message);

    public static ServiceResult BadRequest(string message) =>
        Fail(400, "Bad Request", message);

    public static ServiceResult ValidationError(string message) =>
        Fail(422, "Validation Error", message);

    public static ServiceResult Conflict(string message) =>
        Fail(409, "Conflict", message);

    public static ServiceResult InternalError(string message = "An internal server error occurred") =>
        Fail(500, "Internal Server Error", message);

    // Utility method to convert to generic version
    public ServiceResult<T> ToServiceResult<T>() =>
        Success
            ? ServiceResult<T>.Ok(default(T)!, ResponseStatusCode)
            : ServiceResult<T>.Fail(ResponseStatusCode, ErrorTitle ?? string.Empty, ErrorMessage ?? string.Empty);
}
