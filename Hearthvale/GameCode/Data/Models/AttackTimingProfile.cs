using System.Text.Json.Serialization;

namespace Hearthvale.GameCode.Data.Models;

/// <summary>
/// Describes the frame timing and swing characteristics for a melee attack animation.
/// Frame indices are author-facing (1-based) to match sprite tooling.
/// </summary>
public class AttackTimingProfile
{
    /// <summary>
    /// 1-based frame index that marks the first frame where the hit should become active.
    /// </summary>
    public int? ActiveStartFrame { get; set; }

    /// <summary>
    /// Number of consecutive frames that remain active beginning at <see cref="ActiveStartFrame"/>.
    /// </summary>
    public int? ActiveFrameCount { get; set; }

    /// <summary>
    /// Number of setup frames that play before the active window begins. Overrides the value inferred from <see cref="ActiveStartFrame"/> when supplied.
    /// </summary>
    public int? SetupFrameCount { get; set; }

    /// <summary>
    /// Optional number of recovery frames after the active window. When omitted, recovery is inferred from the animation length.
    /// </summary>
    public int? RecoveryFrameCount { get; set; }

    /// <summary>
    /// Optional rotation amount (degrees) used for the wind-up portion of the swing.
    /// </summary>
    public float? WindUpAngleDegrees { get; set; }

    /// <summary>
    /// Optional rotation amount (degrees) applied during the active slash portion of the swing.
    /// </summary>
    public float? SlashArcDegrees { get; set; }

    /// <summary>
    /// Optional rotation amount (degrees) applied while returning to neutral during recovery. If omitted, the weapon returns linearly to the base rotation.
    /// </summary>
    public float? RecoveryAngleDegrees { get; set; }

    /// <summary>
    /// Optional scalar that stretches or compresses the resulting durations without touching the underlying animation timing.
    /// Values greater than 1 slow the swing; values less than 1 speed it up.
    /// </summary>
    [JsonPropertyName("durationScale")]
    public float? DurationScale { get; set; }

    /// <summary>
    /// Optional override for the total melee range. When provided it bypasses derived calculations.
    /// </summary>
    [JsonPropertyName("rangeOverride")]
    public float? RangeOverride { get; set; }

    /// <summary>
    /// Optional additive buffer applied after computing the grounded weapon reach.
    /// </summary>
    [JsonPropertyName("rangeBuffer")]
    public float? RangeBuffer { get; set; }

    /// <summary>
    /// Optional minimum range enforced for the attack. Defaults to the legacy constant when omitted.
    /// </summary>
    [JsonPropertyName("minRange")]
    public float? MinRange { get; set; }

    /// <summary>
    /// Optional scale applied to the equipped weapon length when projecting the strike radius.
    /// </summary>
    [JsonPropertyName("weaponLengthScale")]
    public float? WeaponLengthScale { get; set; }

    /// <summary>
    /// Optional shape override specifying how the hit volume should be constructed.
    /// </summary>
    public AttackShapeDefinition Shape { get; set; } = new AttackShapeDefinition();

    /// <summary>
    /// Optional defensive body collider shape applied to the attacker while the swing plays.
    /// </summary>
    public AttackShapeDefinition DefensiveBodyShape { get; set; }

    /// <summary>
    /// Optional magic payload triggered when the attack resolves.
    /// </summary>
    public MagicEffectDefinition Magic { get; set; }

    /// <summary>
    /// Optional tag designers can use to categorise attacks (e.g., "slash", "pierce", "spell").
    /// </summary>
    public string Category { get; set; }

    /// <summary>
    /// Optional projectile id to spawn instead of (or in addition to) the melee swing.
    /// </summary>
    public string ProjectileId { get; set; }
}