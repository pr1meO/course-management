namespace CourseManagement.Configuration;

public static class ApplicationBuilderExtension
{
    public static IApplicationBuilder Configure(
        this WebApplication app)
    {
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}
