using System.Text.Json.Nodes;
using Core.Models.Rules;

namespace Core.Helpers;

public static class PayloadPath
{
    public const int MaxSegments = 8;

    private const char SegmentSeparator = '.';
    private const char AccessorOpen = '[';
    private const char AccessorClose = ']';
    private const char Wildcard = '*';

    public static bool TryParse(string path, out List<PathSegment> segments)
    {
        segments = [];

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        foreach (var part in path.Split(SegmentSeparator))
        {
            if (!TryParseSegment(part, out var segment))
            {
                segments = [];

                return false;
            }

            segments.Add(segment);
        }

        return segments.Count <= MaxSegments;
    }

    public static bool IsAddressable(string path)
    {
        return !string.IsNullOrWhiteSpace(path) && (!path.Contains(Wildcard) || TryParse(path, out _));
    }

    public static bool HasWildcard(string path)
    {
        return TryParse(path, out var segments) && segments.Any(x => x.Accessors.Any(accessor => accessor.IsWildcard));
    }

    // Legacy rules address keys containing dots or brackets, so the whole field is tried as a literal key first.
    public static List<JsonNode?> ResolveAll(JsonObject payload, string path)
    {
        if (payload.TryGetPropertyValue(path, out var literal))
        {
            return [literal];
        }

        if (!TryParse(path, out var segments))
        {
            return [];
        }

        var current = new List<JsonNode?> { payload };

        foreach (var segment in segments)
        {
            current = Descend(current, segment);

            if (current.Count == 0)
            {
                return current;
            }
        }

        return current;
    }

    public static JsonNode? ResolveSingle(JsonObject payload, string path)
    {
        return ResolveAll(payload, path).FirstOrDefault();
    }

    private static List<JsonNode?> Descend(List<JsonNode?> nodes, PathSegment segment)
    {
        var resolved = new List<JsonNode?>();

        foreach (var node in nodes)
        {
            if (node is not JsonObject jsonObject || !jsonObject.TryGetPropertyValue(segment.Name, out var value))
            {
                continue;
            }

            resolved.Add(value);
        }

        foreach (var accessor in segment.Accessors)
        {
            resolved = Index(resolved, accessor);
        }

        return resolved;
    }

    private static List<JsonNode?> Index(List<JsonNode?> nodes, PathAccessor accessor)
    {
        var indexed = new List<JsonNode?>();

        foreach (var node in nodes)
        {
            if (node is not JsonArray jsonArray)
            {
                continue;
            }

            if (accessor.IsWildcard)
            {
                indexed.AddRange(jsonArray);

                continue;
            }

            if (accessor.Index < jsonArray.Count)
            {
                indexed.Add(jsonArray[accessor.Index!.Value]);
            }
        }

        return indexed;
    }

    private static bool TryParseSegment(string part, out PathSegment segment)
    {
        segment = new PathSegment { Name = string.Empty, Accessors = [] };

        var open = part.IndexOf(AccessorOpen);
        var name = open < 0 ? part : part[..open];

        if (string.IsNullOrWhiteSpace(name) || name.Contains(AccessorClose) || name.Contains(Wildcard))
        {
            return false;
        }

        var accessors = new List<PathAccessor>();
        var position = open < 0 ? part.Length : open;

        while (position < part.Length)
        {
            if (part[position] != AccessorOpen)
            {
                return false;
            }

            var close = part.IndexOf(AccessorClose, position);

            if (close < 0)
            {
                return false;
            }

            var inner = part[(position + 1)..close];

            if (inner == Wildcard.ToString())
            {
                accessors.Add(new PathAccessor { Index = null, IsWildcard = true });
            }
            else if (int.TryParse(inner, out var index) && index >= 0 && inner.All(char.IsAsciiDigit))
            {
                accessors.Add(new PathAccessor { Index = index, IsWildcard = false });
            }
            else
            {
                return false;
            }

            position = close + 1;
        }

        segment = new PathSegment { Name = name, Accessors = accessors };

        return true;
    }
}
