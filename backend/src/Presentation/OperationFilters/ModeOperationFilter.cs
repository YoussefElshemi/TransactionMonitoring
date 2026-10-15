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
                "**Ignored when authenticating as an API client:** a client's mode is the one its key was " +
                $"issued in, so a `{ModeConstants.Test}` key is served the sandbox whatever this header says. " +
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
    }
}
