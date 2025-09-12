using System;
using Hearthvale.GameCode.Utils;

namespace Hearthvale.GameCode.Systems;

/// <summary>One-shot hero atlas frame bounds preprocessing.</summary>
public sealed class SpriteAnalysisSystem : IGameSystem
{
    private readonly AssetManagerSystem _assets;

    public SpriteAnalysisSystem(AssetManagerSystem assets)
    {
        _assets = assets;
    }

    public void Initialize()
    {
        if (_assets.HeroAtlas?.Texture == null) return;

        foreach (var animName in _assets.HeroAtlas.GetAnimationNames())
        {
            try
            {
                var animation = _assets.HeroAtlas.GetAnimation(animName);
                if (animation?.Frames == null) continue;
                foreach (var f in animation.Frames)
                    SpriteAnalyzer.GetContentBounds(f.Texture, f.SourceRectangle);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sprite analysis error {animName}: {ex.Message}");
            }
        }

    }

    public void Update(Microsoft.Xna.Framework.GameTime gameTime) { }
}