using CourseManagement.Configuration.Constants;
using CourseManagement.Models;
using CourseManagement.Repositories;
using CourseManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Configuration.Extensions;

public static class DependencyInjectionExtension
{
    public static IServiceCollection AddConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers(options => options.SuppressAsyncSuffixInActionNames = false);
        services.AddSwaggerGen();
        services.AddPostgres(configuration);
        services.AddApplicationRepositories();
        services.AddApplicationServices();

        return services;
    }

    private static IServiceCollection AddPostgres(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(builder =>
            builder.UseNpgsql(
                configuration.GetConnectionString(ConnectionStrings.POSTGRES)));

        return services;
    }

    private static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<ITeacherService, TeacherService>();
        services.AddScoped<ICourseService, CourseService>();

        return services;
    }

    private static IServiceCollection AddApplicationRepositories(
    this IServiceCollection services)
    {
        services.AddScoped<ITeachersRepository, TeachersRepository>();
        services.AddScoped<ICoursesRepository, CoursesRepository>();

        return services;
    }
}
