using CourseManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace CourseManagement.Configuration.Extensions;

public static class DatabaseMigrationExtensions
{
    public static async Task ApplyMigrationsAsync(
        this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        try
        {
            AppDbContext appDbContext = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            // Перед использванием метода MigrateAsync
            // необходимо руками прописать Add-Migration Init
            await appDbContext.Database.MigrateAsync();
        }
        catch (Exception exception)
        {
            ILogger<AppDbContext> logger = scope.ServiceProvider
                .GetRequiredService<ILogger<AppDbContext>>();

            logger.LogError(
                exception,
                "Error during database migration.");

            throw;
        }
    }
}