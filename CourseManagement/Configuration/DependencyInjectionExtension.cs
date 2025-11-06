namespace CourseManagement.Configuration;

public static class DependencyInjectionExtension
{
    public static IServiceCollection AddConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers();
        services.AddSwaggerGen();

        return services;
    }
}
