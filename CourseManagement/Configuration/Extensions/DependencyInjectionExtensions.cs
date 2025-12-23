using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using CourseManagement.Configuration.Constants;
using CourseManagement.Configuration.Options;
using CourseManagement.Configuration.Swagger;
using CourseManagement.Contracts;
using CourseManagement.Models;
using CourseManagement.RabbitMq.Consumers;
using CourseManagement.RabbitMq.Producers;
using CourseManagement.RabbitMq.Services;
using CourseManagement.Repositories;
using CourseManagement.Services;
using CourseManagement.Services.Auth;
using IdempotentAPI.Cache.DistributedCache.Extensions.DependencyInjection;
using IdempotentAPI.Core;
using IdempotentAPI.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;

namespace CourseManagement.Configuration.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers(options => options.SuppressAsyncSuffixInActionNames = false);
        services.AddFixedRateLimiter();
        services.AddRabbitMq();
        services.AddIdempotency();
        services.AddSwaggerSetup();
        services.AddRedis(configuration);
        services.AddPostgres(configuration);
        services.AddOptions(configuration);
        services.AddSecurity();
        services.AddDataShaping();
        services.AddJwtAuthentication(configuration);
        services.AddAuthenticationCore();
        services.AddApplicationRepositories();
        services.AddApplicationServices();

        return services;
    }

    private static IServiceCollection AddRabbitMq(
        this IServiceCollection services)
    {
        services.AddSingleton(new RabbitMqOptions
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest",
            VirtualHost = "/",
        });

        services.AddSingleton<IApiMessageConsumer, ApiMessageConsumer>();
        services.AddScoped<IMessageProcessingService, MessageProcessingService>();
        services.AddSingleton<IIdempotencyService, InMemoryIdempotencyService>();

        services.AddSingleton<IRabbitMqProducer, RabbitMqProducer>();

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

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(
                configuration.GetConnectionString(ConnectionStrings.REDIS)!));

        return services;
    }

    private static IServiceCollection AddIdempotency(
        this IServiceCollection services)
    {
        services.AddIdempotentAPI(new IdempotencyOptions());
        services.AddIdempotentAPIUsingDistributedCache();

        return services;
    }

    private static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        TokenOptions settings = configuration
            .GetSection(nameof(TokenOptions))
            .Get<TokenOptions>()!;

        SymmetricSecurityKey securityKey = new(
            Encoding.UTF8.GetBytes(settings.Key));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer("Access", options =>
            {
                options.TokenValidationParameters = new()
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = securityKey,
                };
            });

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

    private static IServiceCollection AddFixedRateLimiter(
        this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Общий лимитер на основе 'фиксированного окна' для всех входящих запросов
            options.GlobalLimiter = PartitionedRateLimiter
                .Create<HttpContext, string>(context =>
                {
                    string? teacherId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

                    return RateLimitPartition.GetFixedWindowLimiter(
                        teacherId
                        ?? context.Connection.RemoteIpAddress?.ToString()
                        ?? context.Request.Headers.Host.ToString(),
                        _ => new FixedWindowRateLimiterOptions()
                        {
                            PermitLimit = 10,
                            QueueLimit = 0,
                            Window = TimeSpan.FromSeconds(10),
                            AutoReplenishment = true,
                        });
                });

            // Этот обработчик вызывается, когда клиент превысил лимит запросов
            options.OnRejected = async (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    // RetryAfter - заголовок: через какое время можно повторить запрос
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);

                    // X-Limit-Remaining - заголовок: количество доступных запросов
                    context.HttpContext.Response.Headers.Append("X-Limit-Remaining", "0");
                }

                ExceptionResponse response = new()
                {
                    StatusCode = StatusCodes.Status429TooManyRequests,
                    Message = "Too many requests. Please try again later.",
                };

                context.HttpContext.Response.ContentType = "application/json";
                context.HttpContext.Response.StatusCode = response.StatusCode;

                await context.HttpContext.Response.WriteAsJsonAsync(
                    response,
                    CancellationToken.None);
            };
        });

        return services;
    }

    private static IServiceCollection AddDataShaping(
        this IServiceCollection services)
    {
        services.AddScoped(typeof(IDataShaper<>), typeof(DataShaper<>));

        return services;
    }

    private static IServiceCollection AddSwaggerSetup(
        this IServiceCollection services)
    {
        services.ConfigureOptions<ConfigureSwaggerOptions>();
        services
            .AddSwaggerGen(options =>
            {
                // Добавляем схему безопасности: JWT Bearer-аутентификацию в Swagger
                options.AddSecurityDefinition(
                    JwtBearerDefaults.AuthenticationScheme,
                    new OpenApiSecurityScheme
                    {
                        In = ParameterLocation.Header,
                        Name = "Authorization",
                        Type = SecuritySchemeType.Http,
                        Scheme = JwtBearerDefaults.AuthenticationScheme,
                    });

                // Используем схему безопасности из AddSecurityDefinition
                // Требуем авторизацию для защищённых ([Authorize]) операций
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme()
                        {
                            Reference = new OpenApiReference()
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = JwtBearerDefaults.AuthenticationScheme,
                            },
                        },
                        Array.Empty<string>()
                    },
                });

                // Добавляем поддержку идемпотентности в документации
                // Вызывается для каждого метода действия (endpoint'а)
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
}
