using Asp.Versioning;
using CourseManagement.Configuration.Constants;
using CourseManagement.Configuration.Options;
using CourseManagement.Configuration.Swagger;
using CourseManagement.Models;
using CourseManagement.Repositories;
using CourseManagement.Services;
using CourseManagement.Services.Auth;
using IdempotentAPI.Cache.DistributedCache.Extensions.DependencyInjection;
using IdempotentAPI.Core;
using IdempotentAPI.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Configuration.Extensions;

public static class DependencyInjectionExtension
{
    public static IServiceCollection AddConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers(options => options.SuppressAsyncSuffixInActionNames = false);
        services.AddIdempotency();
        services.AddSwaggerSetup();
        services.AddRedis(configuration);
        services.AddPostgres(configuration);
        services.AddOptions(configuration);
        services.AddSecurity();
        services.AddAuthenticationCore();
        services.AddApplicationRepositories();
        services.AddApplicationServices();

        return services;
    }

    private static IServiceCollection AddSwaggerSetup(
        this IServiceCollection services)
    {
        services.ConfigureOptions<ConfigureSwaggerOptions>();
        services
            .AddSwaggerGen(options =>
            {
                options.OperationFilter<IdempotencyKeyOperationFilter>();
            })
            .AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1);
                options.ReportApiVersions = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader());
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'V";
                options.SubstituteApiVersionInUrl = true;
            });

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

    private static IServiceCollection AddRedis(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString(ConnectionStrings.REDIS);
        });

        return services;
    }

    private static IServiceCollection AddIdempotency(
        this IServiceCollection services)
    {
        services.AddIdempotentAPI(new IdempotencyOptions());
        services.AddIdempotentAPIUsingDistributedCache();

        return services;
    }

    private static IServiceCollection AddAuthenticationCore(
        this IServiceCollection services)
    {
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ITokenFactory, TokenFactory>();
        services.AddScoped<IClaimProvider, ClaimProvider>();

        return services;
    }

    private static IServiceCollection AddSecurity(
        this IServiceCollection services)
    {
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ISigningService, SigningService>();

        return services;
    }

    private static IServiceCollection AddOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<TokenOptions>(configuration
            .GetSection(nameof(TokenOptions)));

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
