using System.Net;
using System.Security.Authentication;
using CourseManagement.Contracts;

namespace CourseManagement.Middlewares;

public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next.Invoke(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionMessageAsync(context, ex);
        }
    }

    private static async Task HandleExceptionMessageAsync(HttpContext context, Exception exception)
    {
        ExceptionResponse response = exception switch
        {
            KeyNotFoundException _ => new()
            {
                StatusCode = HttpStatusCode.NotFound,
                Message = "The requested resource could not be found.",
            },
            InvalidOperationException _ => new()
            {
                StatusCode = HttpStatusCode.Conflict,
                Message = "A conflict occurred while processing your request.",
            },
            AuthenticationException _ => new()
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Message = "Authentication failed. Please check your credentials and try again.",
            },
            _ => new()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Message = "Internal server error.",
            }
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)response.StatusCode;

        await context.Response.WriteAsJsonAsync(response);
    }
}
