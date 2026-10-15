namespace Infrastructure.Constants;

public static class OpenTelemetryTags
{
    public const string ExceptionType = "exception.type";

    public const string ResourceName = "resource_name";

    public const string Environment = "env";
    public const string HostName = "hostname";
    public const string ServiceName = "service.name";

    public const string QuartzJobName = "quartz.job_name";

    public const string HttpMethod = "http.method";
    public const string HttpHost = "http.host";
    public const string HttpPathGroup = "http.path_group";
    public const string HttpContentType = "http.content_type";
    public const string HttpUrl = "http.url";
    public const string HttpUserAgent = "http.user_agent";
    public const string HttpStatusCode = "http.status_code";
    public const string HttpRoute = "http.route";

    public const string UrlPath = "url.path";

    public const string UrlDetailsHost = "url_details.host";
    public const string UrlDetailsPath = "url_details.path";
    public const string UrlDetailsScheme = "url_details.scheme";

    public const string HttpRequestProtocol = "http.request.protocol";
    public const string HttpRequestContentLength = "http.request.content_length";
    public const string HttpRequestMethod = "http.request.method";

    public const string HttpResponseContentLength = "http.response.content_length";

    public const string CorrelationId = "correlation.id";

    public const string ApiVersion = "api.version";
    public const string ApiVersionSource = "api.version.source";
    public const string ApiVersionDeprecated = "api.version.deprecated";
    public const string ApiCaller = "api.caller";
    public const string OrganizationId = "organization.id";

    public const string DbCommandType = "db.CommandType";
    public const string DbCommandText = "db.CommandText";
    public const string DbIsTransaction = "db.IsTransaction";
}