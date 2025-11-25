using System.Text.Json.Serialization;

namespace Hearthvale.GameCode.Data.Models;

/// <summary>
/// Describes the geometric shape that an attack should occupy during its active window.
/// </summary>
public class AttackShapeDefinition
{
    public AttackShapeKind Type { get; set; } = AttackShapeKind.Arc;

    /// <summary>
    /// Forward extent of the shape in pixels. For box and thrust shapes this is the length projected from the attacker.
    /// </summary>
    public float? Length { get; set; }

    /// <summary>
    /// Lateral extent (perpendicular to facing) used by box and thrust shapes.
    /// </summary>
    public float? Width { get; set; }

    /// <summary>
    /// Vertical extent for box shapes (used for downward slashes to stretch along Y axis).
    /// </summary>
    public float? Height { get; set; }

    /// <summary>
    /// Forward offset from the character center to start the shape. Allows keeping a small gap before the hit begins.
    /// </summary>
    public float? ForwardOffset { get; set; }

    /// <summary>
    /// Optional offset along the perpendicular axis (positive values nudge to the right of facing, negative to the left).
    /// </summary>
    public float? LateralOffset { get; set; }

    /// <summary>
    /// Optional offset applied vertically in world space to better align with animations that strike below the sprite center.
    /// </summary>
    public float? VerticalOffset { get; set; }

    /// <summary>
    /// Radius for circular area effects.
    /// </summary>
    public float? Radius { get; set; }

    /// <summary>
    /// Optional cone angle (degrees) for sector-based area effects. When omitted, full circle is assumed.
    /// </summary>
    public float? ConeAngleDegrees { get; set; }

    /// <summary>
    /// Number of segments to use when approximating circular shapes.
    /// </summary>
    public int? SegmentCount { get; set; }

    /// <summary>
    /// Optional thickness parameter for thrust attacks to control collision breadth.
    /// </summary>
    public float? Thickness { get; set; }

    /// <summary>
    /// Optional identifier to hint at VFX/SFX variations for designers.
    /// </summary>
    public string VisualTag { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AttackShapeKind
{
    Arc,
    Box,
    Thrust,
    Area
}