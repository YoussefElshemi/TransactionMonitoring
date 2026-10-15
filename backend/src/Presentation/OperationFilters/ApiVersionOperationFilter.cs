using Core.Constants;
using Microsoft.OpenApi.Models;
using Presentation.Middlewares;
using Swashbuckle.AspNetCore.SwaggerGen;
using HeaderNames = Presentation.Constants.HeaderNames;

namespace Presentation.OperationFilters;

public class ApiVersionOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = HeaderNames.ApiVersion,
            In = ParameterLocation.Header,
            Required = false,
            Description =
                "Which version of the contract to serve this one request under, as a date " +
                $"(`{ApiVersions.Current}`). Leave it out in normal operation: an API client is served the " +
                "version it is pinned to, which is the one that was current when it was created and does not " +
                "move until you move it. Send it to try a newer version on a single request before changing " +
                "the pin.\n\n" +
                "A value that is not a served version is rejected with 400 and the code " +
                $"`{ErrorCodes.UnsupportedApiVersion}` rather than defaulted. The served versions, the " +
                $"notice period and the rule for what counts as a breaking change are at `{ApiVersionMiddleware.CataloguePath}`.",
            Schema = new OpenApiSchema
            {
                Type = "string",
                Format = "date"
            }
        });

        foreach (var response in operation.Responses)
        {
            response.Value.Headers[HeaderNames.ApiVersion] = new OpenApiHeader
            {
                Description = "The version this response was served under.",
                Schema = new OpenApiSchema { Type = "string", Format = "date" }
            };

            response.Value.Headers[HeaderNames.Deprecation] = new OpenApiHeader
            {
                Description =
                    "Present only when the version served has been deprecated: the moment it was, as an " +
                    "RFC 9745 date.",
                Schema = new OpenApiSchema { Type = "string" }
            };

            response.Value.Headers[HeaderNames.Sunset] = new OpenApiHeader
            {
                Description =
                    "Present only when the version served has been deprecated: the earliest moment it may " +
                    $"stop being served, as an HTTP date. Never less than {ApiVersions.MinimumNoticeMonths} " +
                    "months after the deprecation.",
                Schema = new OpenApiSchema { Type = "string" }
            };

            response.Value.Headers[HeaderNames.CorrelationId] = new OpenApiHeader
            {
                Description =
                    "Identifies this request in the platform's logs. Quote it when reporting a problem; " +
                    "an error body carries the same value as correlationId.",
                Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
            };
        }
    }
}
