using System.Net;
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
            InvalidOperationException _ => new()
            {
                StatusCode = HttpStatusCode.NotFound,
                Message = "The resource was not found.",
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
