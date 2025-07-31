using Microsoft.AspNetCore.Mvc;
using Inventory.Api.Common;

namespace Inventory.Api.Controllers;

// Base controller providing common functionality for all API controllers
[ApiController]
[Route("api/[controller]")]
public abstract class ApiBaseController : ControllerBase
{
    // Handles ServiceResult<T> and converts it to appropriate HTTP response
    protected IActionResult HandleServiceResult<T>(ServiceResult<T> result)
    {
        if (result.Success)
        {
            return result.ResponseStatusCode switch
            {
                201 => Created(string.Empty, result.Data),
                204 => NoContent(),
                _ => Ok(result.Data)
            };
        }

        return HandleServiceError(result);
    }

    // Handles ServiceResult without data and converts it to appropriate HTTP response
    protected IActionResult HandleServiceResult(ServiceResult result)
    {
        if (result.Success)
        {
            return result.ResponseStatusCode switch
            {
                201 => Created(string.Empty, null),
                204 => NoContent(),
                _ => Ok()
            };
        }

        return HandleServiceError(result);
    }

    // Handles service errors and creates standardized error responses with correct RFC types
    private IActionResult HandleServiceError<T>(ServiceResult<T> result)
    {
        var (type, title) = GetProblemDetailsInfo(result.ResponseStatusCode);

        var problemDetails = new
        {
            type,
            title = result.ErrorTitle ?? title,
            status = result.ResponseStatusCode,
            detail = result.ErrorMessage,
            instance = HttpContext?.Request?.Path.Value ?? "unknown"
        };

        return result.ResponseStatusCode switch
        {
            400 => BadRequest(problemDetails),
            401 => Unauthorized(problemDetails),
            403 => Forbid(),
            404 => NotFound(problemDetails),
            409 => Conflict(problemDetails),
            422 => UnprocessableEntity(problemDetails),
            _ => StatusCode(result.ResponseStatusCode, problemDetails)
        };
    }

    // Handles service errors for non-generic ServiceResult and creates standardized error responses with correct RFC types
    private IActionResult HandleServiceError(ServiceResult result)
    {
        var (type, title) = GetProblemDetailsInfo(result.ResponseStatusCode);

        var problemDetails = new
        {
            type,
            title = result.ErrorTitle ?? title,
            status = result.ResponseStatusCode,
            detail = result.ErrorMessage,
            instance = HttpContext?.Request?.Path.Value ?? "unknown"
        };

        return result.ResponseStatusCode switch
        {
            400 => BadRequest(problemDetails),
            401 => Unauthorized(problemDetails),
            403 => Forbid(),
            404 => NotFound(problemDetails),
            409 => Conflict(problemDetails),
            422 => UnprocessableEntity(problemDetails),
            _ => StatusCode(result.ResponseStatusCode, problemDetails)
        };
    }

    // Gets the appropriate RFC 9110 problem type and title for a given status code
    private static (string type, string title) GetProblemDetailsInfo(int statusCode)
    {
        return statusCode switch
        {
            400 => ("https://tools.ietf.org/html/rfc9110#section-15.5.1", "Bad Request"),
            401 => ("https://tools.ietf.org/html/rfc9110#section-15.5.2", "Unauthorized"),
            403 => ("https://tools.ietf.org/html/rfc9110#section-15.5.4", "Forbidden"),
            404 => ("https://tools.ietf.org/html/rfc9110#section-15.5.5", "Not Found"),
            409 => ("https://tools.ietf.org/html/rfc9110#section-15.5.10", "Conflict"),
            422 => ("https://tools.ietf.org/html/rfc4918#section-11.2", "Unprocessable Entity"),
            500 => ("https://tools.ietf.org/html/rfc9110#section-15.6.1", "Internal Server Error"),
            502 => ("https://tools.ietf.org/html/rfc9110#section-15.6.3", "Bad Gateway"),
            503 => ("https://tools.ietf.org/html/rfc9110#section-15.6.4", "Service Unavailable"),
            _ => ("https://tools.ietf.org/html/rfc9110", "Error")
        };
    }
}
