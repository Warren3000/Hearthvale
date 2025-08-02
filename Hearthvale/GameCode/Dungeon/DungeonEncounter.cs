using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Hearthvale.GameCode;

public class DungeonEncounter : IDungeonElement
{
    public string Id { get; }
    public bool IsActive { get; private set; }
    public List<Enemy> Enemies { get; }
    public void Activate() { /* Spawn enemies */ }
    public void Deactivate() { /* End encounter */ }
    public void Update(GameTime gameTime) { /* Encounter logic */ }
}