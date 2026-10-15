using System.Net;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Presentation.Constants;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Presentation.OperationFilters;

public class ModeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = ModeConstants.ModeHeader,
            In = ParameterLocation.Header,
            Required = true,
            Description =
                $"Which partition this request reads and writes: `{ModeConstants.Live}` for real traffic, " +
                $"`{ModeConstants.Test}` for the sandbox. The two share no data — a case, rule or payment " +
                "created in one is invisible from the other. Sandbox traffic is never billed, never files a " +
                "report and never sends an email or chat message, and it spends a monthly credit allowance " +
                "rather than the live meter.\n\n" +
                "Omitting the header is treated as " + $"`{ModeConstants.Live}`. Any other value is rejected " +
                "with 400 rather than defaulted.\n\n" +
                "**API clients are bound to the mode their key was issued in.** A " +
                $"`{ModeConstants.Live}` key may only call `{ModeConstants.Live}` and a `{ModeConstants.Test}` key " +
                "only `" + ModeConstants.Test + "`; naming the other mode is rejected with 403 `mode_mismatch`. " +
                "Omitting the header serves the key's own mode. " +
                "The response always carries the mode the request was actually served under.",
            Schema = new OpenApiSchema
            {
                Type = "string",
                Enum =
                [
                    new OpenApiString(ModeConstants.Live),
                    new OpenApiString(ModeConstants.Test)
                ]
            }
        });

        foreach (var response in operation.Responses)
        {
            response.Value.Headers[ModeConstants.ModeHeader] = new OpenApiHeader
            {
                Description = "The mode this response was served under.",
                Schema = new OpenApiSchema
                {
                    Type = "string"
                }
            };
        }

        operation.Responses.TryAdd(((int)HttpStatusCode.BadRequest).ToString(), new OpenApiResponse
        {
            Description = $"{ModeConstants.ModeHeader} was supplied but is not {ModeConstants.Live} or {ModeConstants.Test}."
        });

        operation.Responses.TryAdd(((int)HttpStatusCode.Forbidden).ToString(), new OpenApiResponse
        {
            Description = $"An API client named a {ModeConstants.ModeHeader} other than the mode its key was issued in."
        });
    }
}
