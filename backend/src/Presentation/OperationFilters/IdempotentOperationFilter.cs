using System.Net;
using Microsoft.OpenApi.Models;
using Presentation.Attributes;
using Presentation.Constants;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Presentation.OperationFilters;

public class IdempotentOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var isIdempotent = context.MethodInfo.GetCustomAttributes(true).OfType<IdempotentAttribute>().Any() ||
                           context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<IdempotentAttribute>().Any() == true;

        if (!isIdempotent)
        {
            return;
        }

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = HeaderNames.IdempotencyKey,
            In = ParameterLocation.Header,
            Required = false,
            Description =
                "A GUID that makes this request safe to retry. Repeating a request with the same key returns the " +
                "original response instead of evaluating again, and that replayed response carries the " +
                $"`{HeaderNames.IdempotentReplay}: true` header (see the 200 response below). " +
                "**Generate a new GUID for every distinct request.** A key is bound to the first endpoint that " +
                "used it, so reusing one on a different rule or rule set returns 409 rather than a replay. Keys " +
                "are scoped to your organisation and are kept for 24 hours.",
            Schema = new OpenApiSchema
            {
                Type = "string",
                Format = "uuid"
            }
        });

        if (!operation.Responses.Any(x => IsSuccess(x.Key)))
        {
            operation.Responses.TryAdd(((int)HttpStatusCode.OK).ToString(), new OpenApiResponse
            {
                Description = HttpStatusCode.OK.ToString()
            });
        }

        foreach (var response in operation.Responses.Where(x => IsSuccess(x.Key)))
        {
            response.Value.Headers[HeaderNames.IdempotentReplay] = new OpenApiHeader
            {
                Description = "Present and set to true when this response was replayed from an earlier request.",
                Schema = new OpenApiSchema
                {
                    Type = "boolean"
                }
            };
        }

        operation.Responses.TryAdd(((int)HttpStatusCode.BadRequest).ToString(), new OpenApiResponse
        {
            Description = $"{HeaderNames.IdempotencyKey} was supplied but is not a GUID."
        });

        operation.Responses.TryAdd(((int)HttpStatusCode.Conflict).ToString(), new OpenApiResponse
        {
            Description =
                $"{HeaderNames.IdempotencyKey} belongs to a request that is still running, or was already used " +
                "for a different endpoint."
        });
    }

    private static bool IsSuccess(string statusCode)
    {
        return int.TryParse(statusCode, out var parsed) && parsed is >= 200 and < 300;
    }
}
