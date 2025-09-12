using Microsoft.Xna.Framework;

namespace Hearthvale.GameCode.Systems;

/// <summary>
/// Basic lifecycle contract for game systems. Draw is optional.
/// </summary>
public interface IGameSystem
{
    /// <summary>Called once after registration (after core + managers initialized).</summary>
    void Initialize();

    /// <summary>Per-frame update.</summary>
    void Update(GameTime gameTime);

    /// <summary>Optional draw hook (only override when needed).</summary>
    void Draw(GameTime gameTime) { }
}