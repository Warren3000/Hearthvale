using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;
using MonoGameLibrary.Graphics;
using Hearthvale.GameCode.Rendering;

namespace Hearthvale.GameCode.Systems;

/// <summary>
/// Loads core assets that were previously handled inside Game1.
/// </summary>
public sealed class AssetManagerSystem : IGameSystem
{
    private readonly ContentManager _content;
    public Song ThemeSong { get; private set; }
    public TextureAtlas HeroAtlas { get; private set; }
    public TextureAtlas DungeonAtlas { get; private set; }

    public AssetManagerSystem(ContentManager content)
    {
        _content = content;
    }

    public void Initialize()
    {
        ThemeSong    = _content.Load<Song>("audio/theme");
        HeroAtlas    = TextureAtlas.FromFile(_content, "images/npc-atlas.xml");
        DungeonAtlas = TextureAtlas.FromFile(_content, "images/chest-definition.xml");

        // Animation names must exist in chest-definition.xml
        DungeonLootRenderer.Initialize(
            DungeonAtlas,
            closedIdleAnimation: "chest-wood_idle0",
            openingAnimation:    "chest-wood_open",
            openedIdleAnimation: "chest-wood_idle1"
        );
    }

    public void Update(Microsoft.Xna.Framework.GameTime gameTime) { }
}