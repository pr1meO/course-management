using CourseManagement.Middlewares;

namespace CourseManagement.Configuration.Extensions;

public static class ApplicationBuilderExtension
{
    public static IApplicationBuilder Configure(
        this WebApplication app)
    {
        //app.UseExceptionHandlerMiddleware();
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }

    private static IApplicationBuilder UseExceptionHandlerMiddleware(
        this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlerMiddleware>();

        return app;
    }
}
