using Hearthvale.GameCode.Entities;
using Hearthvale.GameCode.Entities.NPCs;
using Hearthvale.GameCode.Data;
using Hearthvale.GameCode.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System;
using MonoGameLibrary;

namespace Hearthvale.GameCode.Managers;

/// <summary>
/// Manages all combat logic, including player/NPC attacks and projectile handling with MonoGame.Extended collision detection.
/// </summary>
public class CombatManager
{
    private static CombatManager _instance;
    public static CombatManager Instance => _instance ?? throw new InvalidOperationException("CombatManager not initialized. Call Initialize first.");

    private const float ATTACK_COOLDOWN = 0.5f;
    private const float PLAYER_DAMAGE_COOLDOWN = 1.0f;
    private const float PROJECTILE_KNOCKBACK = 150f;
    private const float NPC_ATTACK_KNOCKBACK = 100f;

    private readonly NpcManager _npcManager;
    private Character _player;
    private readonly ScoreManager _scoreManager;
    private readonly CombatEffectsManager _effectsManager;
    private readonly SoundEffect _hitSound;
    private readonly SoundEffect _defeatSound;
    private readonly List<Projectile> _projectiles = new();
    private Rectangle _worldBounds;
    private readonly SpriteBatch _spriteBatch;

    // Additional sound effects for projectile collisions
    private SoundEffect _projectileHitSound;
    private SoundEffect _projectileWallHitSound;
    private SoundEffect _projectileBlockedSound;

    private float _attackCooldown = ATTACK_COOLDOWN;
    private float _attackTimer = 0f;
    private float _playerDamageCooldown = PLAYER_DAMAGE_COOLDOWN;
    private float _playerDamageTimer = 0f;
    private readonly Dictionary<Character, bool> _npcHitPlayerThisSwing = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CombatManager"/> class.
    /// </summary>
    public CombatManager(
        NpcManager npcManager,
        Character player,
        ScoreManager scoreManager,
        SpriteBatch spriteBatch,
        CombatEffectsManager effectsManager,
        SoundEffect hitSound,
        SoundEffect defeatSound,
        Rectangle worldBounds)
    {
        _npcManager = npcManager;
        _player = player;
        _scoreManager = scoreManager;
        _spriteBatch = spriteBatch;
        _effectsManager = effectsManager;
        _hitSound = hitSound;
        _defeatSound = defeatSound;
        _worldBounds = worldBounds;

        // Load additional sound effects for projectiles
        LoadProjectileSounds();
    }

    private void LoadProjectileSounds()
    {
        try
        {
            _projectileHitSound = Core.Content.Load<SoundEffect>("audio/projectile_hit");
            _projectileWallHitSound = Core.Content.Load<SoundEffect>("audio/projectile_wall_hit");
            _projectileBlockedSound = Core.Content.Load<SoundEffect>("audio/projectile_blocked");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load projectile sounds: {ex.Message}");
            // Use fallback sounds
            _projectileHitSound = _hitSound;
            _projectileWallHitSound = _hitSound;
            _projectileBlockedSound = _hitSound;
        }
    }

    public static void Initialize(
        NpcManager npcManager,
        Character player,
        ScoreManager scoreManager,
        SpriteBatch spriteBatch,
        CombatEffectsManager effectsManager,
        SoundEffect hitSound,
        SoundEffect defeatSound,
        Rectangle worldBounds)
    {
        _instance = new CombatManager(npcManager, player, scoreManager, spriteBatch, effectsManager, hitSound, defeatSound, worldBounds);
    }

    public void Update(GameTime gameTime)
    {
        if (_player == null)
            return;

        float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_attackTimer > 0)
        {
            _attackTimer -= elapsed;
        }

        if (_playerDamageTimer > 0)
            _playerDamageTimer -= elapsed;

        // Update projectiles
        UpdateProjectiles(gameTime);

        // --- NPC Melee Attack Hit Detection ---
        // Use the Character base class attack area system instead of the removed interface method
        foreach (var npc in _npcManager.Npcs.Where(n => !n.IsDefeated))
        {
            if (npc.IsAttacking)
            {
                var weaponPolygon = npc.WeaponComponent?.GetCombatHitPolygon();
                if (weaponPolygon != null && weaponPolygon.Count > 0)
                {
                    var playerPolygon = PolygonIntersection.CreateRectanglePolygon(_player.GetCombatBounds());
                    if (PolygonIntersection.DoPolygonsIntersect(weaponPolygon, playerPolygon))
                    {
                        TryDamagePlayer(npc.AttackPower, npc.Position);
                    }
                }
            }
        }
    }

    private void UpdateProjectiles(GameTime gameTime)
    {
        for (int i = _projectiles.Count - 1; i >= 0; i--)
        {
            var projectile = _projectiles[i];
            projectile.Update(gameTime);

            // Check if projectile is out of bounds
            if (!projectile.IsActive || !_worldBounds.Contains(projectile.Position))
            {
                HandleProjectileOutOfBounds(projectile);
                _npcManager.UnregisterProjectile(projectile);
                _projectiles.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Handles collision between a projectile and an NPC
    /// </summary>
    public void HandleProjectileNpcCollision(Projectile projectile, NPC npc)
    {
        if (projectile.OwnerId == "Player" && !npc.IsDefeated && projectile.CanCollide)
        {
            // Calculate hit direction and knockback
            Vector2 hitDirection = Vector2.Normalize(npc.Position - projectile.Position);
            if (hitDirection.LengthSquared() == 0)
                hitDirection = Vector2.Normalize(projectile.Velocity);

            Vector2 knockback = hitDirection * PROJECTILE_KNOCKBACK;

            // Create collision response based on projectile type
            var response = CreateProjectileCollisionResponse(projectile, npc.Position);

            // Delegate damage handling to NPC component
            bool wasDefeated = npc.HandleProjectileHit(projectile.Damage, knockback);

            // Show visual effects
            ShowProjectileHitEffects(projectile, npc.Position, response);
            
            // Apply status effects
            if (projectile.Type == ProjectileType.Fireball)
                this.ApplyBurnEffect(npc);
            //npc.ApplyStatusEffect("Burn");
            else if (projectile.Type == ProjectileType.Magic)
                this.ApplyMagicEffect(npc);
            //npc.ApplyStatusEffect("Magic");

            // Deactivate projectile (unless it penetrates)
            if (!response.Penetrates)
            {
                projectile.IsActive = false;
                _npcManager.UnregisterProjectile(projectile);
            }
        }
    }

    /// <summary>
    /// Handles collision between a projectile and the player
    /// </summary>
    public void HandleProjectilePlayerCollision(Projectile projectile, Character player)
    {
        if (projectile.OwnerId != "Player" && !player.IsDefeated)
        {
            // Calculate hit direction
            Vector2 hitDirection = Vector2.Normalize(player.Position - projectile.Position);
            if (hitDirection.LengthSquared() == 0)
                hitDirection = Vector2.Normalize(projectile.Velocity);

            // Create collision response
            var response = CreateProjectileCollisionResponse(projectile, player.Position);

            // Check for player blocking/dodging
            if (CanPlayerBlockProjectile(projectile, player))
            {
                HandleProjectileBlocked(projectile, player);
                return;
            }

            // Apply damage to player
            TryDamagePlayer(projectile.Damage, projectile.Position);

            // Show visual effects
            ShowProjectileHitEffects(projectile, player.Position, response);

            // Play sound
            PlayProjectileHitSound(projectile, response);

            // Deactivate projectile
            projectile.IsActive = false;
            _npcManager.UnregisterProjectile(projectile);
        }
    }

    /// <summary>
    /// Handles collision between a projectile and a wall
    /// </summary>
    private void HandleProjectileWallCollision(Projectile projectile)
    {
        // Create collision response
        var response = CreateWallCollisionResponse(projectile);

        // Show impact effects
        ShowProjectileWallImpactEffects(projectile, response);

        // Play wall hit sound
        _projectileWallHitSound?.Play();

        // Handle ricochet if applicable
        if (response.Ricochets)
        {
            HandleProjectileRicochet(projectile);
        }
        else
        {
            // Standard wall collision - destroy projectile
            projectile.IsActive = false;
            _npcManager.UnregisterProjectile(projectile);
        }
    }

    /// <summary>
    /// Handles projectile going out of bounds
    /// </summary>
    private void HandleProjectileOutOfBounds(Projectile projectile)
    {
        // Create a simple fade-out effect for projectiles leaving the screen
        _effectsManager.ShowCombatText(projectile.Position, "Miss", Color.Gray);

        Debug.WriteLine($"Projectile {projectile.Type} went out of bounds at {projectile.Position}");
    }

    /// <summary>
    /// Creates a collision response based on projectile and target
    /// </summary>
    private ProjectileCollisionResponse CreateProjectileCollisionResponse(Projectile projectile, Vector2 hitPosition)
    {
        return projectile.Type switch
        {
            ProjectileType.Arrow => new ProjectileCollisionResponse
            {
                Penetrates = false,
                CreatesSparks = false,
                HasExplosion = false,
                EffectIntensity = 1.0f,
                EffectColor = Color.Yellow
            },
            ProjectileType.Fireball => new ProjectileCollisionResponse
            {
                Penetrates = false,
                CreatesSparks = true,
                HasExplosion = true,
                EffectIntensity = 2.0f,
                EffectColor = Color.Orange,
                ExplosionRadius = 32f
            },
            ProjectileType.Magic => new ProjectileCollisionResponse
            {
                Penetrates = true,
                CreatesSparks = true,
                HasExplosion = false,
                EffectIntensity = 1.5f,
                EffectColor = Color.Purple
            },
            ProjectileType.Bullet => new ProjectileCollisionResponse
            {
                Penetrates = false,
                CreatesSparks = true,
                HasExplosion = false,
                EffectIntensity = 0.8f,
                EffectColor = Color.White
            },
            _ => new ProjectileCollisionResponse
            {
                Penetrates = false,
                CreatesSparks = false,
                HasExplosion = false,
                EffectIntensity = 1.0f,
                EffectColor = Color.White
            }
        };
    }

    /// <summary>
    /// Creates a wall collision response
    /// </summary>
    private WallCollisionResponse CreateWallCollisionResponse(Projectile projectile)
    {
        return projectile.Type switch
        {
            ProjectileType.Bullet => new WallCollisionResponse
            {
                Ricochets = true,
                RicochetCount = 1,
                VelocityRetention = 0.7f,
                CreatesSparks = true,
                EffectColor = Color.White
            },
            ProjectileType.Magic => new WallCollisionResponse
            {
                Ricochets = false,
                CreatesSparks = true,
                EffectColor = Color.Purple,
                HasSpecialEffect = true
            },
            _ => new WallCollisionResponse
            {
                Ricochets = false,
                CreatesSparks = false,
                EffectColor = Color.Gray
            }
        };
    }

    /// <summary>
    /// Shows visual effects for projectile hits
    /// </summary>
    private void ShowProjectileHitEffects(Projectile projectile, Vector2 hitPosition, ProjectileCollisionResponse response)
    {
        // Show damage number
        _effectsManager.ShowCombatText(hitPosition, projectile.Damage.ToString(), response.EffectColor);

        // Show explosion effect if applicable
        if (response.HasExplosion)
        {
            // Create explosion visual effect
            _effectsManager.ShowCombatText(hitPosition, "BOOM!", Color.Red);

            // Handle area damage if explosion has radius
            if (response.ExplosionRadius > 0)
            {
                HandleExplosionDamage(hitPosition, response.ExplosionRadius, projectile.Damage / 2);
            }
        }

        // Show sparks if applicable
        if (response.CreatesSparks)
        {
            // Create spark particle effects
            for (int i = 0; i < 3; i++)
            {
                var sparkOffset = new Vector2(
                    (float)(new Random().NextDouble() - 0.5) * 20,
                    (float)(new Random().NextDouble() - 0.5) * 20
                );
                _effectsManager.ShowCombatText(hitPosition + sparkOffset, "*", response.EffectColor);
            }
        }
    }

    /// <summary>
    /// Shows visual effects for wall impacts
    /// </summary>
    private void ShowProjectileWallImpactEffects(Projectile projectile, WallCollisionResponse response)
    {
        if (response.CreatesSparks)
        {
            // Create wall impact sparks
            for (int i = 0; i < 2; i++)
            {
                var sparkOffset = new Vector2(
                    (float)(new Random().NextDouble() - 0.5) * 15,
                    (float)(new Random().NextDouble() - 0.5) * 15
                );
                _effectsManager.ShowCombatText(projectile.Position + sparkOffset, "�", response.EffectColor);
            }
        }

        // Show impact text
        _effectsManager.ShowCombatText(projectile.Position, "CLANG", Color.Gray);
    }

    /// <summary>
    /// Handles explosion damage to nearby entities
    /// </summary>
    private void HandleExplosionDamage(Vector2 explosionCenter, float radius, int damage)
    {
        // Damage NPCs in explosion radius
        foreach (var npc in _npcManager.Npcs.Where(n => !n.IsDefeated))
        {
            float distance = Vector2.Distance(explosionCenter, npc.Position);
            if (distance <= radius)
            {
                // Calculate damage falloff based on distance
                float damageMultiplier = 1.0f - (distance / radius);
                int explosionDamage = (int)(damage * damageMultiplier);

                Vector2 knockbackDirection = Vector2.Normalize(npc.Position - explosionCenter);
                Vector2 knockback = knockbackDirection * PROJECTILE_KNOCKBACK * damageMultiplier;

                HandleNpcHit(npc, explosionDamage, knockback);
            }
        }

        // Damage player if in radius
        float playerDistance = Vector2.Distance(explosionCenter, _player.Position);
        if (playerDistance <= radius)
        {
            float damageMultiplier = 1.0f - (playerDistance / radius);
            int explosionDamage = (int)(damage * damageMultiplier);
            TryDamagePlayer(explosionDamage, explosionCenter);
        }
    }

    /// <summary>
    /// Handles projectile ricochet off walls
    /// </summary>
    private void HandleProjectileRicochet(Projectile projectile)
    {
        // Simple ricochet - reverse X or Y velocity based on collision
        // This is a simplified version - you might want more sophisticated collision normal detection

        // For now, just reverse the velocity component that's largest
        if (Math.Abs(projectile.Velocity.X) > Math.Abs(projectile.Velocity.Y))
        {
            projectile.Velocity = new Vector2(-projectile.Velocity.X * 0.7f, projectile.Velocity.Y * 0.7f);
        }
        else
        {
            projectile.Velocity = new Vector2(projectile.Velocity.X * 0.7f, -projectile.Velocity.Y * 0.7f);
        }

        // Show ricochet effect
        _effectsManager.ShowCombatText(projectile.Position, "PING!", Color.Yellow);
    }

    /// <summary>
    /// Handles special projectile effects based on type
    /// </summary>
    private void HandleSpecialProjectileEffects(Projectile projectile, NPC npc, ProjectileCollisionResponse response)
    {
        switch (projectile.Type)
        {
            case ProjectileType.Fireball:
                // Fireball might apply burn effect
                ApplyBurnEffect(npc);
                break;
            case ProjectileType.Magic:
                // Magic projectile might have mana drain or special effects
                ApplyMagicEffect(npc);
                break;
        }
    }

    /// <summary>
    /// Checks if player can block incoming projectile
    /// </summary>
    private bool CanPlayerBlockProjectile(Projectile projectile, Character player)
    {
        // Simple blocking logic - you can expand this
        // For now, assume player has a small chance to block based on equipped weapon
        return false; // Implement blocking logic here
    }

    /// <summary>
    /// Handles when a projectile is blocked by the player
    /// </summary>
    private void HandleProjectileBlocked(Projectile projectile, Character player)
    {
        _projectileBlockedSound?.Play();
        _effectsManager.ShowCombatText(player.Position, "BLOCKED!", Color.Blue);

        projectile.IsActive = false;
        _npcManager.UnregisterProjectile(projectile);
    }

    /// <summary>
    /// Plays appropriate sound for projectile hit
    /// </summary>
    private void PlayProjectileHitSound(Projectile projectile, ProjectileCollisionResponse response)
    {
        if (response.HasExplosion)
        {
            // Play explosion sound (if you have one)
            _projectileHitSound?.Play();
        }
        else
        {
            _projectileHitSound?.Play();
        }
    }

    /// <summary>
    /// Applies burn effect to NPC (placeholder)
    /// </summary>
    private void ApplyBurnEffect(NPC npc)
    {
        // Implement burn damage over time here
        Debug.WriteLine($"Applied burn effect to {npc.Name}");
    }

    /// <summary>
    /// Applies magic effect to NPC (placeholder)
    /// </summary>
    private void ApplyMagicEffect(NPC npc)
    {
        // Implement magic effects here (slow, confusion, etc.)
        Debug.WriteLine($"Applied magic effect to {npc.Name}");
    }

    public bool CanAttack() => _attackTimer <= 0f;

    public void SetPlayer(Character player)
    {
        _player = player;
    }

    public void StartCooldown()
    {
        _attackTimer = _attackCooldown;
    }

    public void RegisterProjectile(Projectile projectile)
    {
        if (projectile != null)
        {
            _projectiles.Add(projectile);
            _npcManager.RegisterProjectile(projectile);
        }
    }

    public void TryDamagePlayer(int amount, Vector2 attackerPosition)
    {
        if (_playerDamageTimer > 0 || _player.IsDefeated)
            return;

        Vector2 direction = Vector2.Normalize(_player.Position - attackerPosition);
        if (direction.LengthSquared() == 0)
        {
            direction = Vector2.UnitX;
        }

        float knockbackStrength = NPC_ATTACK_KNOCKBACK;
        Vector2 knockback = direction * knockbackStrength;
        _player.TakeDamage(amount, knockback);

        _playerDamageTimer = _playerDamageCooldown;
        _effectsManager.ShowCombatText(_player.Position, amount.ToString(), Color.Red);
        _hitSound?.Play();
    }

    public bool HandleNpcHit(Character npc, int damage, Vector2? knockback = null)
    {
        Debug.WriteLine($"[CombatManager] HandleNpcHit called for '{npc.GetType().Name}' with {damage} damage.");
        bool justDefeated = npc.TakeDamage(damage, knockback);
        _effectsManager.ShowCombatText(npc.Position, damage.ToString(), Color.Yellow);
        _hitSound?.Play();

        Debug.WriteLine($"[CombatManager] 'justDefeated' returned: {justDefeated}");
        if (justDefeated)
        {
            Debug.WriteLine("[CombatManager] justDefeated is TRUE. Granting XP.");
            _defeatSound?.Play();
            ScoreManager.Instance.Add(1);

            if (npc is NPC typedNpc)
            {
                var stats = DataManager.Instance.GetCharacterStats(typedNpc.Name);
                _player.EquippedWeapon?.GainXP(stats.XpYield);
            }
        }

        return justDefeated;
    }

    public void DrawProjectiles(SpriteBatch spriteBatch)
    {
        foreach (var projectile in _projectiles)
        {
            projectile.Draw(spriteBatch);
        }
    }
}

/// <summary>
/// Defines the response behavior when a projectile hits a target
/// </summary>
public class ProjectileCollisionResponse
{
    public bool Penetrates { get; set; } = false;
    public bool CreatesSparks { get; set; } = false;
    public bool HasExplosion { get; set; } = false;
    public float EffectIntensity { get; set; } = 1.0f;
    public Color EffectColor { get; set; } = Color.White;
    public float ExplosionRadius { get; set; } = 0f;
}

/// <summary>
/// Defines the response behavior when a projectile hits a wall
/// </summary>
public class WallCollisionResponse
{
    public bool Ricochets { get; set; } = false;
    public int RicochetCount { get; set; } = 0;
    public float VelocityRetention { get; set; } = 1.0f;
    public bool CreatesSparks { get; set; } = false;
    public Color EffectColor { get; set; } = Color.Gray;
    public bool HasSpecialEffect { get; set; } = false;
}