using System.Text.Json.Serialization;

namespace Hearthvale.GameCode.Data.Models;

/// <summary>
/// Optional payload allowing melee profiles to trigger spell-like secondary effects.
/// </summary>
public class MagicEffectDefinition
{
    public MagicEffectKind Type { get; set; } = MagicEffectKind.None;

    /// <summary>
    /// Identifier used to look up VFX, SFX, and gameplay payloads in higher-level systems.
    /// </summary>
    public string EffectId { get; set; }

    /// <summary>
    /// Radius for area-based spell payloads.
    /// </summary>
    public float? Radius { get; set; }

    /// <summary>
    /// Optional width for linear spell payloads (e.g., walls, beams).
    /// </summary>
    public float? Width { get; set; }

    /// <summary>
    /// Optional duration in seconds for lingering effects.
    /// </summary>
    public float? DurationSeconds { get; set; }

    /// <summary>
    /// Optional tick cadence for damage-over-time payloads.
    /// </summary>
    public float? TickIntervalSeconds { get; set; }

    /// <summary>
    /// Optional maximum number of targets affected per activation.
    /// </summary>
    public int? MaxTargets { get; set; }

    /// <summary>
    /// Optional scalar applied to base damage when the effect resolves.
    /// </summary>
    public float? DamageScale { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MagicEffectKind
{
    None,
    AreaOfEffect,
    Projectile,
    Chain,
    Buff,
    Debuff
}