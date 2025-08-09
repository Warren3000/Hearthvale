using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Collision;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Hearthvale.GameCode.Entities;


namespace Hearthvale.GameCode.Managers
{
    /// <summary>
    /// Coordinates NPC updates with collision detection
    /// </summary>
    public class NpcUpdateCoordinator
    {
        private readonly CollisionWorldManager _collisionManager;

        public NpcUpdateCoordinator(CollisionWorldManager collisionManager)
        {
            _collisionManager = collisionManager;
        }

        public void UpdateNpc(NPC npc, GameTime gameTime, Character player, List<NPC> allNpcs)
        {
            Vector2 oldPosition = npc.Position;

            // Get nearby walls for NPC collision detection
            var nearbyWalls = _collisionManager.GetNearbyWalls(npc.Position);

            // Update the NPC
            npc.Update(gameTime, allNpcs, player, nearbyWalls);

            // Update collision position if NPC moved
            if (npc.Position != oldPosition)
            {
                _collisionManager.UpdateNpcPosition(npc);
            }
        }
    }
}