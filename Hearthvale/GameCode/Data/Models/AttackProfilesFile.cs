using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hearthvale.GameCode.Data.Models;

/// <summary>
/// Represents the root document for attack profile configuration including schema metadata.
/// Supports both the new wrapped format and the legacy flat dictionary format.
/// </summary>
public class AttackProfilesFile
{
    private static readonly JsonSerializerOptions DefaultSerializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    [JsonPropertyName("profiles")]
    public Dictionary<string, AttackTimingProfile> Profiles { get; set; }
        = new Dictionary<string, AttackTimingProfile>(StringComparer.OrdinalIgnoreCase);

    [JsonExtensionData]
    public Dictionary<string, JsonElement> LegacyProfiles { get; set; }
        = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Resolves the attack profiles from either the wrapped or legacy file structure.
    /// </summary>
    public Dictionary<string, AttackTimingProfile> ResolveProfiles(JsonSerializerOptions options = null)
    {
        if (Profiles is { Count: > 0 })
        {
            return Profiles;
        }

        if (LegacyProfiles is not { Count: > 0 })
        {
            return new Dictionary<string, AttackTimingProfile>(StringComparer.OrdinalIgnoreCase);
        }

        var resolved = new Dictionary<string, AttackTimingProfile>(StringComparer.OrdinalIgnoreCase);
        var serializerOptions = options ?? DefaultSerializerOptions;

        foreach (var (key, element) in LegacyProfiles)
        {
            if (string.IsNullOrWhiteSpace(key) || element.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            try
            {
                var profile = element.Deserialize<AttackTimingProfile>(serializerOptions);
                if (profile != null)
                {
                    resolved[key] = profile;
                }
            }
            catch (JsonException)
            {
                // Ignore malformed legacy entries; DataManager will log if necessary.
            }
        }

        return resolved;
    }
}