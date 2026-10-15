using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;

namespace Core.Exceptions;

[Serializable]
public abstract partial class DomainException : Exception
{
    private static readonly ConcurrentDictionary<Type, string> Codes = new();

    protected DomainException() { }
    protected DomainException(string? message) : base(message) { }
    protected DomainException(string? message, Exception innerException) : base(message, innerException) { }

    public abstract HttpStatusCode GetStatusCode();

    public abstract string GetTitle();

    public virtual string GetCode()
    {
        return Codes.GetOrAdd(GetType(), ToCode);
    }

    public virtual object? GetExtensions()
    {
        return null;
    }

    public static string ToCode(Type exceptionType)
    {
        var name = exceptionType.Name.EndsWith(nameof(Exception), StringComparison.Ordinal)
            ? exceptionType.Name[..^nameof(Exception).Length]
            : exceptionType.Name;

        return WordBoundary().Replace(name, "$1_$2").ToLowerInvariant();
    }

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex WordBoundary();
}
