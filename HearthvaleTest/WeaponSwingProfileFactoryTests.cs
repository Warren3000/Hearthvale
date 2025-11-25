using Hearthvale.GameCode.Data.Models;
using Hearthvale.GameCode.Entities;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;

namespace HearthvaleTest;

public class WeaponSwingProfileFactoryTests
{
    [Fact]
    public void FromAttackTiming_ComputesDurationsAndAngles()
    {
        var timing = new AttackTimingProfile
        {
            ActiveStartFrame = 8,
            ActiveFrameCount = 1,
            SetupFrameCount = 7,
            RecoveryFrameCount = 2,
            WindUpAngleDegrees = 18f,
            SlashArcDegrees = 35f,
            RecoveryAngleDegrees = 10f,
            Shape = new AttackShapeDefinition
            {
                Type = AttackShapeKind.Box,
                Length = 38,
                Width = 18
            },
            Magic = new MagicEffectDefinition
            {
                Type = MagicEffectKind.AreaOfEffect,
                Radius = 24,
                DamageScale = 0.4f
            }
        };

        var animation = new Animation(CreateFrames(count: 10), TimeSpan.FromSeconds(0.1));

        var profile = WeaponSwingProfileFactory.FromAttackTiming(timing, animation);

        Assert.Equal(0.7f, profile.WindUpDuration, 3);
        Assert.Equal(0.1f, profile.ActiveDuration, 3);
        Assert.Equal(0.2f, profile.RecoveryDuration, 3);
        Assert.Equal(MathHelper.ToRadians(18f), profile.WindUpAngleRadians, 5);
        Assert.Equal(MathHelper.ToRadians(35f), profile.SlashArcRadians, 5);
        Assert.Equal(MathHelper.ToRadians(10f), profile.RecoveryAngleRadians, 5);
        Assert.Equal(AttackShapeKind.Box, profile.Shape.Type);
        Assert.Equal(38f, profile.Shape.Length.GetValueOrDefault(), 3);
        Assert.Equal(18f, profile.Shape.Width.GetValueOrDefault(), 3);
        Assert.NotNull(profile.Magic);
        Assert.Equal(MagicEffectKind.AreaOfEffect, profile.Magic!.Type);
        Assert.Equal(24f, profile.Magic.Radius.GetValueOrDefault(), 3);
    }

    [Fact]
    public void FromAttackTiming_AppliesDurationScale()
    {
        var timing = new AttackTimingProfile
        {
            ActiveStartFrame = 5,
            ActiveFrameCount = 2,
            SetupFrameCount = 4,
            DurationScale = 1.25f
        };

        const float frameSeconds = 0.08f;
        var animation = new Animation(CreateFrames(count: 8), TimeSpan.FromSeconds(frameSeconds));

        var profile = WeaponSwingProfileFactory.FromAttackTiming(timing, animation);

        Assert.Equal(4 * frameSeconds * timing.DurationScale!.Value, profile.WindUpDuration, 3);
        Assert.Equal(2 * frameSeconds * timing.DurationScale.Value, profile.ActiveDuration, 3);
        Assert.Equal(2 * frameSeconds * timing.DurationScale.Value, profile.RecoveryDuration, 3);
    }

    private static List<TextureRegion> CreateFrames(int count)
    {
        var frames = new List<TextureRegion>(count);
        for (int i = 0; i < count; i++)
        {
            frames.Add(new TextureRegion());
        }
        return frames;
    }
}