using Hearthvale.GameCode.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hearthvale.GameCode.Rendering
{
    /// <summary>
    /// Renders animated dungeon loot (chests). Replaces static closed/open region approach.
    /// </summary>
    public static class DungeonLootRenderer
    {
        private static TextureAtlas _atlas;
        private static string _closedIdleAnim;
        private static string _openingAnim;
        private static string _openedIdleAnim;
        private static bool _initialized;
        private static readonly List<DungeonLoot> _registered = new();

        public static void Initialize(TextureAtlas atlas, string closedIdleAnimation, string openingAnimation, string openedIdleAnimation = null)
        {
            _atlas = atlas;
            _closedIdleAnim = closedIdleAnimation;
            _openingAnim = openingAnimation;
            _openedIdleAnim = openedIdleAnimation;
            _initialized = true;

            // Initialize any loot already constructed before atlas readiness
            foreach (var loot in _registered)
            {
                loot.InitializeAnimations(_atlas, _closedIdleAnim, _openingAnim, _openedIdleAnim);
            }
        }

        /// <summary>
        /// Called by DungeonLoot constructor so animation setup can occur once atlas is ready.
        /// </summary>
        public static void Register(DungeonLoot loot)
        {
            if (!_registered.Contains(loot))
                _registered.Add(loot);

            if (_initialized && _atlas != null)
            {
                loot.InitializeAnimations(_atlas, _closedIdleAnim, _openingAnim, _openedIdleAnim);
            }
        }
        /// <summary>
        /// Draw all chests at a uniform layer depth (convenience overload).
        /// </summary>
        public static void Draw(SpriteBatch spriteBatch, IEnumerable<DungeonLoot> chests, float layerDepth)
        {
            if (!_initialized || spriteBatch == null) return;

            layerDepth = MathHelper.Clamp(layerDepth, 0f, 1f);

            foreach (var chest in chests)
            {
                var sprite = chest.GetSprite();
                if (sprite == null) continue;

                var bounds = chest.Bounds;
                sprite.Position = new Vector2(bounds.X, bounds.Y);
                sprite.LayerDepth = layerDepth;
                sprite.Draw(spriteBatch);
            }
        }
        /// <summary>
        /// Draw all chests using a per-chest layer depth selector (e.g., y-sorted: row / maxRows).
        /// </summary>
        public static void Draw(SpriteBatch spriteBatch, IEnumerable<DungeonLoot> chests, Func<DungeonLoot, float> layerDepthSelector)
        {
            if (!_initialized || spriteBatch == null || layerDepthSelector == null) return;

            foreach (var chest in chests)
            {
                var sprite = chest.GetSprite();
                if (sprite == null) continue;

                var bounds = chest.Bounds;
                sprite.Position = new Vector2(bounds.X, bounds.Y);
                sprite.LayerDepth = MathHelper.Clamp(layerDepthSelector(chest), 0f, 1f);
                sprite.Draw(spriteBatch);
            }
        }
        /// <summary>
        /// Convenience draw: fetches all DungeonLoot from the DungeonManager and draws them.
        /// Row-based depth sorting (simple Y -> depth).
        /// </summary>
        public static void DrawFromManager(SpriteBatch spriteBatch, float baseDepth = 0.45f, float perRowIncrement = 0.00005f)
        {
            if (!_initialized || spriteBatch == null) return;
            var manager = DungeonManager.Instance;
            var chests = manager.GetElements<DungeonLoot>();

            Draw(spriteBatch, chests, loot =>
            {
                // Higher rows slightly deeper (or invert depending on your convention)
                return MathHelper.Clamp(baseDepth + loot.Row * perRowIncrement, 0f, 1f);
            });
        }
    }
}