using Core.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Extensions;

public static class ProblemDetailsExtensions
{
    public const string Code = "code";
    public const string CorrelationId = "correlationId";

    public static TProblemDetails WithContract<TProblemDetails>(
        this TProblemDetails problemDetails,
        HttpContext httpContext,
        string code)
        where TProblemDetails : ProblemDetails
    {
        problemDetails.Extensions[Code] = code;
        problemDetails.Extensions[CorrelationId] = httpContext.TraceIdentifier;

        return problemDetails;
    }

    public static void EnsureContract(this ProblemDetails problemDetails, HttpContext httpContext)
    {
        if (!problemDetails.Extensions.ContainsKey(Code))
        {
            var statusCode = problemDetails.Status ?? httpContext.Response.StatusCode;

            problemDetails.Extensions[Code] = problemDetails is ValidationProblemDetails
                ? ErrorCodes.ValidationFailed
                : ErrorCodes.FromStatusCode(statusCode);
        }

        problemDetails.Extensions[CorrelationId] = httpContext.TraceIdentifier;
    }
}
