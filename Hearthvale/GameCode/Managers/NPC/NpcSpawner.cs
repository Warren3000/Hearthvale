using Hearthvale.GameCode.Data.Atlases;
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
        private readonly TextureAtlas _fallbackNpcAtlas;
        private readonly TextureAtlas _weaponAtlas;
        private readonly TextureAtlas _arrowAtlas;
        private readonly WeaponManager _weaponManager;
        private readonly INpcAtlasCatalog _npcAtlasCatalog;

        public NpcSpawner(TextureAtlas heroAtlas, TextureAtlas fallbackNpcAtlas, TextureAtlas weaponAtlas, TextureAtlas arrowAtlas, WeaponManager weaponManager, INpcAtlasCatalog npcAtlasCatalog)
        {
            _heroAtlas = heroAtlas ?? throw new ArgumentNullException(nameof(heroAtlas));
            _fallbackNpcAtlas = fallbackNpcAtlas;
            _weaponAtlas = weaponAtlas ?? throw new ArgumentNullException(nameof(weaponAtlas));
            _arrowAtlas = arrowAtlas ?? throw new ArgumentNullException(nameof(arrowAtlas));
            _weaponManager = weaponManager ?? throw new ArgumentNullException(nameof(weaponManager));
            _npcAtlasCatalog = npcAtlasCatalog ?? NullNpcAtlasCatalog.Instance;
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
                var animations = ResolveAnimations(config.ManifestId, config.AnimationPrefix);

                if (animations.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"No animations found for NPC type: {npcType}");
                    return null;
                }

                SoundEffect defeatSound = Core.Content.Load<SoundEffect>("audio/npc_defeat");

                var npc = new NPC(npcType, animations, spawnPos, bounds, defeatSound, config.Health);

                // Equip weapon to the NPC
                var weapon = new Weapon("Dagger-Copper", DataManager.Instance.GetWeaponStats("Dagger-Copper"), _weaponAtlas, _arrowAtlas);
                _weaponManager.EquipWeapon(npc, weapon);

                return npc;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating NPC '{npcType}': {ex.Message}");
                return null;
            }
        }

        private Dictionary<string, Animation> ResolveAnimations(string npcManifestId, string animationPrefix)
        {
            if (!string.IsNullOrWhiteSpace(npcManifestId) && _npcAtlasCatalog.TryGetDefinition(npcManifestId, out var definition))
            {
                var mapped = definition.CreateAnimations();
                if (mapped.Count > 0)
                {
                    return mapped;
                }
            }

            if (_fallbackNpcAtlas == null)
            {
                return new Dictionary<string, Animation>(StringComparer.OrdinalIgnoreCase);
            }

            return CreateFallbackAnimations(_fallbackNpcAtlas, animationPrefix);
        }

        private Dictionary<string, Animation> CreateFallbackAnimations(TextureAtlas atlas, string animationPrefix)
        {
            var animations = new Dictionary<string, Animation>();

            try
            {
                // Add all 4-directional idle and run animations
                string[] animNames = new[]
                {
                    "Idle_Down", "Idle_Up", "Idle_Right", "Idle_Left",
                    "Run_Down", "Run_Up", "Run_Right", "Run_Left",
                    "Jump_Down", "Jump_Up", "Jump_Right", "Jump_Left",
                    "Land_Down", "Land_Up", "Land_Right", "Land_Left",
                    "Attack_Down", "Attack_Up", "Attack_Right", "Attack_Left",
                    "Death_Down", "Death_Up", "Death_Right", "Death_Left",
                    "Hurt_Down", "Hurt_Up", "Hurt_Right", "Hurt_Left"
                };
                foreach (var anim in animNames)
                {
                    string key = $"{anim}";
                    string resolved = key;

                    if (!string.IsNullOrEmpty(animationPrefix))
                    {
                        string prefixed = $"{key}";
                        if (atlas.HasAnimation(prefixed))
                        {
                            resolved = prefixed;
                        }
                    }

                    if (atlas.HasAnimation(resolved))
                    {
                        animations[anim] = CloneAnimation(atlas.GetAnimation(resolved));
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Warning: Missing animation {resolved}");
                    }
                }

                // Add optional animations if they exist
                string climbKey = $"Climb";
                if (atlas.HasAnimation(climbKey))
                {
                    animations["Climb"] = CloneAnimation(atlas.GetAnimation(climbKey));
                }

                string attack02Key = $"Attack_02";
                if (atlas.HasAnimation(attack02Key))
                {
                    animations["Attack_02"] = CloneAnimation(atlas.GetAnimation(attack02Key));
                }

                // Ensure we have at least one animation
                if (animations.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"No valid animations found");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating animations: {ex.Message}");
            }

            return animations;
        }

        private static Animation CloneAnimation(Animation source)
        {
            return new Animation(new List<TextureRegion>(source.Frames), source.Delay);
        }
    }

    /// <summary>
    /// Configuration data for different NPC types
    /// </summary>
    internal static class NpcConfiguration
    {
        private static readonly Dictionary<string, NpcConfig> Configurations = new()
        {
            { "skeleton", new NpcConfig("skeleton_grunt", "Skeleton", 10) },
            { "goblin", new NpcConfig("goblin", "Goblin", 15) },
            { "warrior", new NpcConfig("warrior_hero", "Warrior", 30) },
            { "defaultnpctype", new NpcConfig("skeleton_grunt", "Skeleton", 10) }
        };

        public static NpcConfig GetConfiguration(string npcType)
        {
            if (string.IsNullOrEmpty(npcType))
                return new NpcConfig("skeleton_grunt", "Skeleton", 10);

            string key = npcType.ToLower();
            return Configurations.TryGetValue(key, out var config) ? config : new NpcConfig("skeleton_grunt", "Skeleton", 10);
        }
    }

    internal sealed record NpcConfig(string ManifestId, string AnimationPrefix, int Health);

    /// <summary>
    /// Provides available NPC types
    /// </summary>
    internal static class NpcTypes
    {
        public static string[] GetAllTypes()
        {
            return new[]
            {
                "Skeleton",
                "Goblin",
                "Warrior"
            };
        }
    }
}