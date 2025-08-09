using Hearthvale.GameCode.Entities;
using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using MonoGame.Extended.Tiled;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;

namespace Hearthvale.GameCode.Managers
{
    /// <summary>
    /// Handles the creation and configuration of NPCs
    /// </summary>
    public class NpcSpawner
    {
        private readonly TextureAtlas _heroAtlas;
        private readonly TextureAtlas _weaponAtlas;
        private readonly TextureAtlas _arrowAtlas;
        private readonly WeaponManager _weaponManager;

        public NpcSpawner(TextureAtlas heroAtlas, TextureAtlas weaponAtlas, TextureAtlas arrowAtlas, WeaponManager weaponManager)
        {
            _heroAtlas = heroAtlas ?? throw new ArgumentNullException(nameof(heroAtlas));
            _weaponAtlas = weaponAtlas ?? throw new ArgumentNullException(nameof(weaponAtlas));
            _arrowAtlas = arrowAtlas ?? throw new ArgumentNullException(nameof(arrowAtlas));
            _weaponManager = weaponManager ?? throw new ArgumentNullException(nameof(weaponManager));
        }

        public NPC CreateNPC(string npcType, Vector2 spawnPos, Rectangle bounds, Tilemap tilemap)
        {
            if (string.IsNullOrEmpty(npcType))
            {
                System.Diagnostics.Debug.WriteLine("Cannot create NPC: npcType is null or empty");
                return null;
            }

            try
            {
                var config = NpcConfiguration.GetConfiguration(npcType);
                var animations = CreateAnimations(config.AnimationPrefix);

                if (animations.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"No animations found for NPC type: {npcType}");
                    return null;
                }

                SoundEffect defeatSound = Core.Content.Load<SoundEffect>("audio/npc_defeat");

                var npc = new NPC(npcType, animations, spawnPos, bounds, defeatSound, config.Health);

                // Equip weapon to the NPC
                var weapon = new Weapon("Dagger", DataManager.Instance.GetWeaponStats("Dagger"), _weaponAtlas, _arrowAtlas);
                _weaponManager.EquipWeapon(npc, weapon);

                return npc;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating NPC '{npcType}': {ex.Message}");
                return null;
            }
        }

        private Dictionary<string, Animation> CreateAnimations(string animationPrefix)
        {
            var animations = new Dictionary<string, Animation>();

            try
            {
                // Check for combined Idle+Walk animation first
                string combinedKey = $"{animationPrefix}_Idle+Walk";
                if (_heroAtlas.HasAnimation(combinedKey))
                {
                    var combinedAnim = _heroAtlas.GetAnimation(combinedKey);
                    animations["Idle"] = combinedAnim;
                    animations["Walk"] = combinedAnim;
                }
                else
                {
                    // Try to get individual animations
                    string idleKey = $"{animationPrefix}_Idle";
                    string walkKey = $"{animationPrefix}_Walk";

                    if (_heroAtlas.HasAnimation(idleKey))
                    {
                        animations["Idle"] = _heroAtlas.GetAnimation(idleKey);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Warning: Missing Idle animation for {animationPrefix}");
                    }

                    if (_heroAtlas.HasAnimation(walkKey))
                    {
                        animations["Walk"] = _heroAtlas.GetAnimation(walkKey);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Warning: Missing Walk animation for {animationPrefix}");
                    }
                }

                // Add optional animations if they exist
                string defeatedKey = $"{animationPrefix}_Defeated";
                if (_heroAtlas.HasAnimation(defeatedKey))
                {
                    animations["Defeated"] = _heroAtlas.GetAnimation(defeatedKey);
                }

                string hitKey = $"{animationPrefix}_Hit";
                if (_heroAtlas.HasAnimation(hitKey))
                {
                    animations["Hit"] = _heroAtlas.GetAnimation(hitKey);
                }

                // Ensure we have at least one animation
                if (animations.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"No valid animations found for {animationPrefix}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating animations for {animationPrefix}: {ex.Message}");
            }

            return animations;
        }
    }

    /// <summary>
    /// Configuration data for different NPC types
    /// </summary>
    internal static class NpcConfiguration
    {
        private static readonly Dictionary<string, (string AnimationPrefix, int Health)> Configurations = new()
        {
            { "merchant", ("Merchant", 8) },
            { "mage", ("Mage", 12) },
            { "archer", ("Archer", 10) },
            { "blacksmith", ("Blacksmith", 10) },
            { "knight", ("Knight", 20) },
            { "heavyknight", ("HeavyKnight", 10) },
            { "fatnun", ("FatNun", 10) },
            { "defaultnpctype", ("Merchant", 10) } // Fallback for the lambda in GameScene
        };

        public static (string AnimationPrefix, int Health) GetConfiguration(string npcType)
        {
            if (string.IsNullOrEmpty(npcType))
                return ("Merchant", 10);

            string key = npcType.ToLower();
            return Configurations.TryGetValue(key, out var config) ? config : ("Merchant", 10);
        }
    }

    /// <summary>
    /// Provides available NPC types
    /// </summary>
    internal static class NpcTypes
    {
        public static string[] GetAllTypes()
        {
            return new[]
            {
                "merchant", "mage", "archer", "blacksmith",
                "knight", "heavyknight", "fatnun"
            };
        }
    }
}