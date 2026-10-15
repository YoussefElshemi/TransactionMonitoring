using System.Net;

namespace Core.Constants;

public static class ErrorCodes
{
    public const string ValidationFailed = "validation_failed";
    public const string BadRequest = "bad_request";
    public const string Unauthorized = "unauthorized";
    public const string Forbidden = "forbidden";
    public const string NotFound = "not_found";
    public const string Conflict = "conflict";
    public const string Gone = "gone";
    public const string UnprocessableEntity = "unprocessable_entity";
    public const string RateLimited = "rate_limited";
    public const string InternalError = "internal_error";
    public const string ServiceUnavailable = "service_unavailable";
    public const string RequestFailed = "request_failed";

    public const string InvalidMode = "invalid_mode";
    public const string InvalidIdempotencyKey = "invalid_idempotency_key";
    public const string IdempotencyRequestInProgress = "idempotency_request_in_progress";
    public const string IdempotencyKeyReused = "idempotency_key_reused";
    public const string UnsupportedApiVersion = "unsupported_api_version";

    public static string FromStatusCode(int statusCode)
    {
        return (HttpStatusCode)statusCode switch
        {
            HttpStatusCode.BadRequest => BadRequest,
            HttpStatusCode.Unauthorized => Unauthorized,
            HttpStatusCode.Forbidden => Forbidden,
            HttpStatusCode.NotFound => NotFound,
            HttpStatusCode.Conflict => Conflict,
            HttpStatusCode.Gone => Gone,
            HttpStatusCode.UnprocessableEntity => UnprocessableEntity,
            HttpStatusCode.TooManyRequests => RateLimited,
            HttpStatusCode.ServiceUnavailable => ServiceUnavailable,
            _ => statusCode >= 500 ? InternalError : RequestFailed
        };
    }
}
