using Hearthvale.GameCode.Data.Models;
using Microsoft.Xna.Framework;

namespace Hearthvale.GameCode.Entities;

/// <summary>
/// Encapsulates the rotation and timing parameters for a single melee swing.
/// </summary>
public sealed class WeaponSwingProfile
{
    public static WeaponSwingProfile Default { get; } = new(
        windUpDuration: 0.15f,
        activeDuration: 0.10f,
        recoveryDuration: 0.08f,
        windUpAngleRadians: MathHelper.PiOver4,
        slashArcRadians: MathHelper.PiOver2,
        recoveryAngleRadians: MathHelper.PiOver4,
        shape: new AttackShapeDefinition
        {
            Type = AttackShapeKind.Arc
        }
    );

    public float WindUpDuration { get; }
    public float ActiveDuration { get; }
    public float RecoveryDuration { get; }
    public float WindUpAngleRadians { get; }
    public float SlashArcRadians { get; }
    public float RecoveryAngleRadians { get; }
    public AttackShapeDefinition Shape { get; }
    public AttackShapeDefinition DefensiveBodyShape { get; }
    public MagicEffectDefinition Magic { get; }
    public float WeaponLengthScale { get; }
    public AttackTimingProfile SourceProfile { get; }

    public WeaponSwingProfile(
        float windUpDuration,
        float activeDuration,
        float recoveryDuration,
        float windUpAngleRadians,
        float slashArcRadians,
        float recoveryAngleRadians,
        AttackShapeDefinition shape = null,
        AttackShapeDefinition defensiveBodyShape = null,
        MagicEffectDefinition magic = null,
        float weaponLengthScale = 1.0f,
        AttackTimingProfile sourceProfile = null)
    {
        WindUpDuration = MathHelper.Max(0f, windUpDuration);
        ActiveDuration = MathHelper.Max(0f, activeDuration);
        RecoveryDuration = MathHelper.Max(0f, recoveryDuration);
        WindUpAngleRadians = MathHelper.Clamp(windUpAngleRadians, -MathHelper.TwoPi, MathHelper.TwoPi);
        SlashArcRadians = MathHelper.Clamp(slashArcRadians, -MathHelper.TwoPi, MathHelper.TwoPi);
        RecoveryAngleRadians = MathHelper.Clamp(recoveryAngleRadians, -MathHelper.TwoPi, MathHelper.TwoPi);
        Shape = shape ?? new AttackShapeDefinition { Type = AttackShapeKind.Arc };
        DefensiveBodyShape = defensiveBodyShape;
        Magic = magic;
        WeaponLengthScale = weaponLengthScale;
        SourceProfile = sourceProfile;
    }

    public WeaponSwingProfile WithDurations(float windUp, float active, float recovery)
    {
        return new WeaponSwingProfile(
            windUp,
            active,
            recovery,
            WindUpAngleRadians,
            SlashArcRadians,
            RecoveryAngleRadians,
            Shape,
            DefensiveBodyShape,
            Magic,
            WeaponLengthScale,
            SourceProfile);
    }

    public WeaponSwingProfile WithAngles(float windUpAngle, float slashArc, float recoveryAngle)
    {
        return new WeaponSwingProfile(
            WindUpDuration,
            ActiveDuration,
            RecoveryDuration,
            windUpAngle,
            slashArc,
            recoveryAngle,
            Shape,
            DefensiveBodyShape,
            Magic,
            WeaponLengthScale,
            SourceProfile);
    }

    public WeaponSwingProfile WithShape(AttackShapeDefinition shape)
    {
        return new WeaponSwingProfile(
            WindUpDuration,
            ActiveDuration,
            RecoveryDuration,
            WindUpAngleRadians,
            SlashArcRadians,
            RecoveryAngleRadians,
            shape,
            DefensiveBodyShape,
            Magic,
            WeaponLengthScale,
            SourceProfile);
    }

    public WeaponSwingProfile WithDefensiveBodyShape(AttackShapeDefinition defensiveShape)
    {
        return new WeaponSwingProfile(
            WindUpDuration,
            ActiveDuration,
            RecoveryDuration,
            WindUpAngleRadians,
            SlashArcRadians,
            RecoveryAngleRadians,
            Shape,
            defensiveShape,
            Magic,
            WeaponLengthScale,
            SourceProfile);
    }

    public WeaponSwingProfile WithMagic(MagicEffectDefinition magic)
    {
        return new WeaponSwingProfile(
            WindUpDuration,
            ActiveDuration,
            RecoveryDuration,
            WindUpAngleRadians,
            SlashArcRadians,
            RecoveryAngleRadians,
            Shape,
            DefensiveBodyShape,
            magic,
            WeaponLengthScale,
            SourceProfile);
    }
}