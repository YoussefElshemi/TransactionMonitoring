using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Presentation.OperationFilters;

public class ProblemDetailsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var problemDetails = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository);
        var validationProblemDetails = context.SchemaGenerator.GenerateSchema(typeof(ValidationProblemDetails), context.SchemaRepository);

        foreach (var response in operation.Responses.Where(x => IsError(x.Key) && x.Value.Content.Count == 0))
        {
            SetContent(response.Value, problemDetails);
        }

        Describe(operation, HttpStatusCode.BadRequest, validationProblemDetails, "The request failed validation.");

        if (RequiresAuthorization(context))
        {
            Describe(operation, HttpStatusCode.Unauthorized, null, "Authentication is missing, expired or invalid.");
            Describe(operation, HttpStatusCode.Forbidden, null, "The caller is authenticated but not allowed to perform this operation.");
        }

        if (context.ApiDescription.ParameterDescriptions.Any(x => x.Source == BindingSource.Path))
        {
            Describe(operation, HttpStatusCode.NotFound, problemDetails, "The requested resource does not exist.");
        }

        Describe(operation, HttpStatusCode.InternalServerError, problemDetails, "An unexpected error occurred.");
    }

    private static void Describe(OpenApiOperation operation, HttpStatusCode statusCode, OpenApiSchema? schema, string description)
    {
        var key = ((int)statusCode).ToString();

        if (operation.Responses.TryGetValue(key, out var existing))
        {
            if (schema != null && existing.Content.Count == 0)
            {
                SetContent(existing, schema);
            }

            return;
        }

        var response = new OpenApiResponse { Description = description };

        if (schema != null)
        {
            SetContent(response, schema);
        }

        operation.Responses.Add(key, response);
    }

    private static void SetContent(OpenApiResponse response, OpenApiSchema schema)
    {
        response.Content["application/problem+json"] = new OpenApiMediaType { Schema = schema };
    }

    private static bool RequiresAuthorization(OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

        return !metadata.OfType<IAllowAnonymous>().Any() && metadata.OfType<IAuthorizeData>().Any();
    }

    private static bool IsError(string statusCode)
    {
        return int.TryParse(statusCode, out var parsed) && parsed >= 400;
    }
}
