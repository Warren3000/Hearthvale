using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Hearthvale.GameCode.Systems;

/// <summary>
/// Static coordinator for registered systems. Keeps ordering explicit.
/// </summary>
public static class SystemManager
{
    private static readonly List<IGameSystem> _updateSystems = new();
    private static readonly List<IGameSystem> _drawSystems = new();
    private static bool _initialized;

    public static void Register(IGameSystem system, bool participatesInDraw = false)
    {
        _updateSystems.Add(system);
        if (participatesInDraw)
            _drawSystems.Add(system);
    }

    public static void InitializeAll()
    {
        if (_initialized) return;
        _initialized = true;
        foreach (var s in _updateSystems)
            s.Initialize();
    }

    public static void UpdateAll(GameTime gameTime)
    {
        for (int i = 0; i < _updateSystems.Count; i++)
            _updateSystems[i].Update(gameTime);
    }

    public static void DrawAll(GameTime gameTime)
    {
        for (int i = 0; i < _drawSystems.Count; i++)
            _drawSystems[i].Draw(gameTime);
    }
}