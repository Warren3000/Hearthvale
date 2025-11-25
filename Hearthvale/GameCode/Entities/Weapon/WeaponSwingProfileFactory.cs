using System;
using Hearthvale.GameCode.Data.Models;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;

namespace Hearthvale.GameCode.Entities;

public static class WeaponSwingProfileFactory
{
    private const float DefaultFrameDuration = 1f / 12f; // Fallback to 12 FPS when metadata is missing

    public static WeaponSwingProfile FromAttackTiming(AttackTimingProfile timing, Animation animation)
    {
        if (timing == null || animation == null)
        {
            return WeaponSwingProfile.Default;
        }

        var frameDuration = (float)animation.Delay.TotalSeconds;
        if (frameDuration <= 0f)
        {
            frameDuration = DefaultFrameDuration;
        }

        var frameCount = animation.Frames?.Count ?? 0;
        if (frameCount <= 0)
        {
            return WeaponSwingProfile.Default;
        }

        // Resolve setup and active window
        int activeStart = Math.Max(0, (timing.ActiveStartFrame ?? 1) - 1);
        if (activeStart >= frameCount)
        {
            activeStart = Math.Max(0, frameCount - 1);
        }

        int setupFrames = timing.SetupFrameCount.HasValue
            ? Math.Clamp(timing.SetupFrameCount.Value, 0, frameCount)
            : activeStart;

        int activeFrames = timing.ActiveFrameCount.HasValue
            ? Math.Max(0, timing.ActiveFrameCount.Value)
            : Math.Max(1, frameCount - setupFrames);

        if (setupFrames + activeFrames > frameCount)
        {
            activeFrames = Math.Max(1, frameCount - setupFrames);
        }

        if (activeFrames <= 0)
        {
            Log.Warn(LogArea.Weapon, "[WeaponSwingProfile] Active frame count resolved to zero. Falling back to default profile.");
            return WeaponSwingProfile.Default;
        }

        int recoveryFrames;
        if (timing.RecoveryFrameCount.HasValue)
        {
            recoveryFrames = Math.Clamp(timing.RecoveryFrameCount.Value, 0, frameCount);
        }
        else
        {
            int consumed = setupFrames + activeFrames;
            recoveryFrames = Math.Max(0, frameCount - consumed);
        }

        float durationScale = timing.DurationScale.HasValue && timing.DurationScale.Value > 0f
            ? timing.DurationScale.Value
            : 1f;

        float windUpDuration = setupFrames * frameDuration * durationScale;
        float activeDuration = activeFrames * frameDuration * durationScale;
        float recoveryDuration = recoveryFrames * frameDuration * durationScale;

        var windUpRadians = timing.WindUpAngleDegrees.HasValue
            ? MathHelper.ToRadians(timing.WindUpAngleDegrees.Value)
            : WeaponSwingProfile.Default.WindUpAngleRadians;

        var slashArcRadians = timing.SlashArcDegrees.HasValue
            ? MathHelper.ToRadians(timing.SlashArcDegrees.Value)
            : WeaponSwingProfile.Default.SlashArcRadians;

        var recoveryAngleRadians = timing.RecoveryAngleDegrees.HasValue
            ? MathHelper.ToRadians(timing.RecoveryAngleDegrees.Value)
            : WeaponSwingProfile.Default.RecoveryAngleRadians;

        var shape = CloneShape(timing.Shape) ?? new AttackShapeDefinition { Type = AttackShapeKind.Arc };
        if (shape.Type == AttackShapeKind.Arc)
        {
            // Ensure arcs inherit slash arc when explicit length was not provided.
            shape.Length ??= timing.RangeOverride;
        }

        var defensiveShape = CloneShape(timing.DefensiveBodyShape);

        return new WeaponSwingProfile(
            windUpDuration,
            activeDuration,
            recoveryDuration,
            windUpRadians,
            slashArcRadians,
            recoveryAngleRadians,
            shape,
            defensiveShape,
            CloneMagic(timing.Magic),
            timing.WeaponLengthScale ?? 1.0f,
            timing);
    }

    private static AttackShapeDefinition CloneShape(AttackShapeDefinition source)
    {
        if (source == null)
        {
            return null;
        }

        return new AttackShapeDefinition
        {
            Type = source.Type,
            Length = source.Length,
            Width = source.Width,
            Height = source.Height,
            ForwardOffset = source.ForwardOffset,
            LateralOffset = source.LateralOffset,
            VerticalOffset = source.VerticalOffset,
            Radius = source.Radius,
            ConeAngleDegrees = source.ConeAngleDegrees,
            SegmentCount = source.SegmentCount,
            Thickness = source.Thickness,
            VisualTag = source.VisualTag
        };
    }

    private static MagicEffectDefinition CloneMagic(MagicEffectDefinition source)
    {
        if (source == null)
        {
            return null;
        }

        return new MagicEffectDefinition
        {
            Type = source.Type,
            EffectId = source.EffectId,
            Radius = source.Radius,
            Width = source.Width,
            DurationSeconds = source.DurationSeconds,
            TickIntervalSeconds = source.TickIntervalSeconds,
            MaxTargets = source.MaxTargets,
            DamageScale = source.DamageScale
        };
    }
}