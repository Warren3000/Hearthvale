using Microsoft.Xna.Framework;
using MonoGameLibrary.Input;

namespace Hearthvale.GameCode.Systems;

/// <summary>
/// Wraps input polling in a system.
/// </summary>
public sealed class InputSystem : IGameSystem
{
    private readonly InputManager _input = new();
    public void Initialize() { }
    public void Update(GameTime gameTime) => _input.Update(gameTime);
}