using Hearthvale.GameCode.Entities;
using Hearthvale.GameCode.Entities.Players;
using Hearthvale.GameCode.Entities.NPCs;
using System.Collections.Generic;
using Hearthvale.GameCode.Entities.Characters;
using Microsoft.Xna.Framework;

namespace Hearthvale.GameCode.Managers
{
    /// <summary>
    /// Manages weapon assignment and swapping for players and NPCs.
    /// </summary>
    public class WeaponManager
    {
        // Optionally track all weapons if needed for inventory, pooling, etc.
        private readonly Dictionary<object, Weapon> _equippedWeapons = new();

        /// <summary>
        /// Equip a weapon to a player.
        /// </summary>
        public void EquipWeapon(Player player, Weapon weapon)
        {
            if (player == null || weapon == null) return;
            player.EquipWeapon(weapon);
            _equippedWeapons[player] = weapon;
        }

        /// <summary>
        /// Equip a weapon to an NPC.
        /// </summary>
        public void EquipWeapon(NPC npc, Weapon weapon)
        {
            if (npc == null || weapon == null) return;
            npc.EquipWeapon(weapon);
            _equippedWeapons[npc] = weapon;
        }

        public void EquipWeapon(Character character, Weapon weapon)
        {
            if (character == null || weapon == null) return;
            character.EquipWeapon(weapon);

            // Force weapon to reset its animation and region
            weapon.SetAnimation("Idle");
            weapon.Sprite.Position = character.Position + new Vector2(character.Sprite.Width / 2, character.Sprite.Height / 2);
            weapon.Sprite.Rotation = weapon.Rotation;
        }

        /// <summary>
        /// Swap the weapon of a player.
        /// </summary>
        public void SwapWeapon(Player player, Weapon newWeapon)
        {
            EquipWeapon(player, newWeapon);
        }

        /// <summary>
        /// Swap the weapon of an NPC.
        /// </summary>
        public void SwapWeapon(NPC npc, Weapon newWeapon)
        {
            EquipWeapon(npc, newWeapon);
        }

        /// <summary>
        /// Get the currently equipped weapon for a player or NPC.
        /// </summary>
        public Weapon GetEquippedWeapon(object entity)
        {
            _equippedWeapons.TryGetValue(entity, out var weapon);
            return weapon;
        }
    }
}
