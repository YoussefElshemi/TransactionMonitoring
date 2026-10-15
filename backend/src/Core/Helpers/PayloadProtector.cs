using System.Text.Json.Nodes;
using Core.Extensions;
using Core.ValueObjects;

namespace Core.Helpers;

public static class PayloadProtector
{
    public static JsonObject Read(DataKey? dataKey, OrganizationId organizationId, string stored)
    {
        if (!PayloadCipher.IsProtected(stored))
        {
            return stored.FromBase64String().TryDeserialize<JsonObject>();
        }

        if (!dataKey.HasValue)
        {
            throw new InvalidOperationException(
                $"A protected payload for organisation '{organizationId}' cannot be read without that organisation's data key.");
        }

        return PayloadCipher.Unprotect(dataKey.Value, organizationId, stored).TryDeserialize<JsonObject>();
    }

    public static string Write(DataKey dataKey, OrganizationId organizationId, JsonObject payload)
    {
        return PayloadCipher.Protect(dataKey, organizationId, payload.Serialize());
    }
}
