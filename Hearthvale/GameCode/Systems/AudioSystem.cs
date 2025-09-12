using Microsoft.Xna.Framework;
using MonoGameLibrary.Audio;

namespace Hearthvale.GameCode.Systems;

/// <summary>
/// Handles audio update & starts background music.
/// </summary>
public sealed class AudioSystem : IGameSystem
{
    private readonly AssetManagerSystem _assets;
    private readonly AudioController _controller = new();

    public AudioSystem(AssetManagerSystem assets) => _assets = assets;

    public void Initialize()
    {
        if (_assets.ThemeSong != null)
        {
            _controller.PlaySong(_assets.ThemeSong);
        }
    }

    public void Update(GameTime gameTime)
    {
        _controller.Update();
    }
}