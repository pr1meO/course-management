using IdempotentAPI.Filters;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CourseManagement.Configuration.Swagger;

public class IdempotencyKeyOperationFilter : IOperationFilter
{
    public void Apply(
        OpenApiOperation operation,
        OperationFilterContext context)
    {
        if (operation.Parameters == null)
            operation.Parameters = new List<OpenApiParameter>();

        if (!context.MethodInfo.GetCustomAttributes(true)
            .OfType<IdempotentAttribute>()
            .Any())
            return;

        operation.Parameters.Add(new()
        {
            Name = "IdempotencyKey",
            In = ParameterLocation.Header,
            Required = true,
            Schema = new()
            {
                Type = "string",
                Format = "uuid",
            },
        });
    }
}
