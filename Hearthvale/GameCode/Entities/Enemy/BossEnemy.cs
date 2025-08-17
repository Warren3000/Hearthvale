using Hearthvale.GameCode.Data.Models;
using Hearthvale.GameCode.Entities;
using Hearthvale.GameCode.Entities.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using MonoGameLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class BossEnemy : Enemy
{
    private List<BossPhase> _phases;
    private BossPhase _currentPhase;
    private int _phaseIndex;
    private float _phaseTransitionThreshold = 0.3f;
    private bool _isInvulnerable;
    private float _enrageTimer;
    private readonly float _enrageThreshold = 300f;
    private bool _isEnraged;

    // Special abilities
    private List<SpecialAbility> _phaseAbilities;
    private Dictionary<string, float> _abilityCooldowns = new();
    private float _globalCooldown;

    // Arena control
    public Rectangle ArenaArea { get; private set; }
    private List<Vector2> _spawnPoints = new();
    private List<Rectangle> _hazardAreas = new();

    // Boss UI elements
    public string BossName { get; }
    public string CurrentPhaseTitle => _currentPhase?.Title ?? "Dormant";
    public float PhaseProgress => _currentPhase?.Progress ?? 0f;
    public bool IsIntroPlaying { get; private set; }

    public BossEnemy(
        string name,
        Dictionary<string, Animation> animations,
        Vector2 position,
        Rectangle bounds,
        SoundEffect soundEffect,
        int maxHealth,
        Rectangle arenaArea,
        EnemyData bossData)
        : base(name, animations, position, bounds, soundEffect, maxHealth)
    {
        ArenaArea = arenaArea;
        BossName = name;
        IsBoss = true;

        // Initialize phases based on boss data
        InitializePhases(bossData);
        InitializeAbilities(bossData);

        // Set initial phase
        if (_phases.Count > 0)
        {
            _currentPhase = _phases[0];
            _phaseIndex = 0;
        }
    }

    private void InitializePhases(EnemyData bossData)
    {
        _phases = new List<BossPhase>
        {
            new IntroPhase(this, "Awakening"),
            new CombatPhase(this, "First Form", bossData.Abilities.Take(2).ToList()),
            new TransitionPhase(this, "Transforming"),
            new EnragedPhase(this, "Final Form", bossData.Abilities.Skip(2).ToList())
        };
    }

    private void InitializeAbilities(EnemyData bossData)
    {
        _phaseAbilities = bossData.Abilities ?? new List<SpecialAbility>();
    }

    public void UpdateAbilityCooldowns(float deltaTime)
    {
        if (_globalCooldown > 0)
            _globalCooldown -= deltaTime;

        var expiredCooldowns = new List<string>();
        foreach (var kvp in _abilityCooldowns)
        {
            _abilityCooldowns[kvp.Key] -= deltaTime;
            if (_abilityCooldowns[kvp.Key] <= 0)
                expiredCooldowns.Add(kvp.Key);
        }

        foreach (var key in expiredCooldowns)
            _abilityCooldowns.Remove(key);
    }

    private void TryUseAbilities()
    {
        if (_globalCooldown > 0) return;

        foreach (var ability in _phaseAbilities.Where(a => CanUseAbility(a)))
        {
            if (new Random().NextDouble() < ability.Chance)
            {
                UseAbility(ability);
                break; // Only use one ability per update
            }
        }
    }

    private bool CanUseAbility(SpecialAbility ability)
    {
        return !_abilityCooldowns.ContainsKey(ability.Id) && !_isInvulnerable;
    }

    private void UseAbility(SpecialAbility ability)
    {
        _abilityCooldowns[ability.Id] = ability.Cooldown;
        _globalCooldown = 1.0f; // Global cooldown between abilities

        // Handle ability effects based on type
        switch (ability.Type)
        {
            case AbilityType.Summon:
                HandleSummonAbility(ability);
                break;
            case AbilityType.AreaEffect:
                HandleAreaEffect(ability);
                break;
                // Add other ability types as needed
        }
    }

    private void HandleSummonAbility(SpecialAbility ability)
    {
        if (ability.Effects.TryGetValue("minion_count", out float count))
        {
            for (int i = 0; i < count; i++)
            {
                // Use spawn points if available, otherwise use random positions
                Vector2 spawnPos = _spawnPoints.Count > 0
                    ? _spawnPoints[new Random().Next(_spawnPoints.Count)]
                    : Position + new Vector2(new Random().Next(-100, 100), new Random().Next(-100, 100));

                // Actual spawning would be handled by NpcManager
                // NpcManager.Instance.SpawnNPC("minion", spawnPos);
            }
        }
    }

    private void HandleAreaEffect(SpecialAbility ability)
    {
        if (ability.Effects.TryGetValue("radius", out float radius) &&
            ability.Effects.TryGetValue("damage", out float damage))
        {
            // Create hazard area
            var hazardArea = new Rectangle(
                (int)(Position.X - radius),
                (int)(Position.Y - radius),
                (int)(radius * 2),
                (int)(radius * 2)
            );
            _hazardAreas.Add(hazardArea);

            // Schedule hazard removal
            // You might want to implement a more sophisticated timing system
            Task.Delay(TimeSpan.FromSeconds(2))
                .ContinueWith(_ => _hazardAreas.Remove(hazardArea));
        }
    }

    public virtual void Update(GameTime gameTime, IEnumerable<NPC> allNpcs = null, Character player = null, IEnumerable<Rectangle> rectangles = null)
    {
        base.Update(gameTime, allNpcs, player, rectangles);

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        //UpdatePhase(deltaTime);
        UpdateAbilityCooldowns(deltaTime);
        UpdateEnrageTimer(deltaTime);

        if (!IsDefeated && !_isInvulnerable)
        {
            TryUseAbilities();
        }

        // Update current phase
        _currentPhase?.Update(deltaTime);
    }

    public override bool TakeDamage(int amount, Vector2? knockback = null)
    {
        if (_isInvulnerable) return false;

        bool wasDefeated = base.TakeDamage(amount, knockback);

        // Check for phase transition
        float healthPercentage = this.Health / (float)MaxHealth;
        if (healthPercentage <= _phaseTransitionThreshold && _phaseIndex < _phases.Count - 1)
        {
            TransitionToNextPhase();
        }

        if (wasDefeated)
        {
            OnBossDefeated();
        }

        return wasDefeated;
    }

    private void TransitionToNextPhase()
    {
        _phaseIndex++;
        if (_phaseIndex < _phases.Count)
        {
            _currentPhase = _phases[_phaseIndex];
            _isInvulnerable = true;
            PlayPhaseTransition();
            //ResetPhaseAbilities();
        }
    }

    private void OnBossDefeated()
    {
        // Handle boss defeat - perhaps drop special loot or trigger an event
        IsIntroPlaying = false;
        _isInvulnerable = false;
        // Additional defeat logic
    }

    // Boss Phase implementations
    private abstract class BossPhase
    {
        protected BossEnemy Owner { get; }
        public string Title { get; protected set; }
        public float Progress { get; protected set; }

        protected BossPhase(BossEnemy owner, string title)
        {
            Owner = owner;
            Title = title;
        }

        public abstract void Update(float deltaTime);
    }

    private class IntroPhase : BossPhase
    {
        private float _introTimer = 2.0f;

        public IntroPhase(BossEnemy owner, string title) : base(owner, title) { }

        public override void Update(float deltaTime)
        {
            _introTimer -= deltaTime;
            Progress = 1 - (_introTimer / 2.0f);

            if (_introTimer <= 0)
            {
                Owner._isInvulnerable = false;
                Owner.TransitionToNextPhase();
            }
        }
    }

    private class CombatPhase : BossPhase
    {
        private readonly List<SpecialAbility> _phaseAbilities;

        public CombatPhase(BossEnemy owner, string title, List<SpecialAbility> abilities)
            : base(owner, title)
        {
            _phaseAbilities = abilities;
        }

        public override void Update(float deltaTime)
        {
            Progress = Owner.Health / (float)Owner.MaxHealth;
        }
    }

    private class TransitionPhase : BossPhase
    {
        private float _transitionTimer = 1.5f;

        public TransitionPhase(BossEnemy owner, string title) : base(owner, title) { }

        public override void Update(float deltaTime)
        {
            _transitionTimer -= deltaTime;
            Progress = 1 - (_transitionTimer / 1.5f);

            if (_transitionTimer <= 0)
            {
                Owner._isInvulnerable = false;
                Owner.TransitionToNextPhase();
            }
        }
    }

    private class EnragedPhase : BossPhase
    {
        private readonly List<SpecialAbility> _phaseAbilities;

        public EnragedPhase(BossEnemy owner, string title, List<SpecialAbility> abilities)
            : base(owner, title)
        {
            _phaseAbilities = abilities;
        }

        public override void Update(float deltaTime)
        {
            Progress = Owner.Health / (float)Owner.MaxHealth;

            // Increased aggression in enraged phase
            foreach (var ability in _phaseAbilities)
            {
                ability.Cooldown *= 0.8f;
            }
        }
    }
    //private void UpdatePhase(float deltaTime)
    //{
    //    // If there are no phases defined or boss is defeated, exit early
    //    if (_phases == null || _phases.Count == 0 || IsDefeated)
    //        return;

    //    // Update current phase progress
    //    _currentPhase?.Update(deltaTime);

    //    // Handle phase transitions based on health thresholds
    //    float healthPercentage = Health / (float)MaxHealth;

    //    // Check for phase transitions based on health thresholds and current phase
    //    switch (_phaseIndex)
    //    {
    //        case 0: // Intro Phase
    //                // Transition out of intro phase happens in IntroPhase.Update
    //            break;

    //        case 1: // First Combat Phase
    //            if (healthPercentage <= 0.6f) // 60% health triggers transition
    //            {
    //                TransitionToNextPhase();
    //                PlayPhaseTransitionEffects("transition_to_second");
    //            }
    //            break;

    //        case 2: // Transition Phase
    //                // Transition happens automatically after timer in TransitionPhase.Update
    //            break;

    //        case 3: // Final/Enraged Phase
    //            if (healthPercentage <= 0.2f && !_isEnraged) // 20% health triggers enrage
    //            {
    //                EnterEnrageMode();
    //                PlayPhaseTransitionEffects("enrage");
    //            }
    //            break;
    //    }

    //    // Update phase-specific mechanics
    //    //UpdatePhaseAbilities(deltaTime);
    //}

    //private void UpdatePhaseAbilities(float deltaTime)
    //{
    //    if (_isInvulnerable || IsDefeated)
    //        return;

    //    // Update ability cooldowns
    //    foreach (var ability in _phaseAbilities)
    //    {
    //        // Check if ability is allowed in current phase
    //        if (!IsAbilityAllowedInCurrentPhase(ability))
    //            continue;

    //        // Attempt to use ability if conditions are met
    //        if (CanUseAbility(ability))
    //        {
    //            float useChance = _isEnraged ? ability.Chance * 1.5f : ability.Chance;
    //            if (new Random().NextDouble() < useChance)
    //            {
    //                UseAbility(ability);
    //            }
    //        }
    //    }
    //}

    //private bool IsAbilityAllowedInCurrentPhase(SpecialAbility ability)
    //{
    //    // Check phase-specific ability restrictions
    //    return _currentPhase switch
    //    {
    //        IntroPhase => false, // No abilities during intro
    //        TransitionPhase => false, // No abilities during transition
    //        CombatPhase combatPhase => combatPhase._phaseAbilities.Contains(ability),
    //        EnragedPhase enragedPhase => enragedPhase._phaseAbilities.Contains(ability),
    //        _ => true
    //    };
    //}

    private void PlayPhaseTransitionEffects(string transitionType)
    {
        // Set invulnerability during transition
        _isInvulnerable = true;

        // Visual effects
        Flash();

        // Play transition sound effect if available
        // _transitionSound?.Play();

        // Spawn transition particles or effects
        switch (transitionType)
        {
            case "transition_to_second":
                // Spawn transition effects
                break;
            case "enrage":
                // Spawn enrage effects
                break;
        }

        // Reset invulnerability after a short delay
        Task.Delay(TimeSpan.FromSeconds(1.5f))
            .ContinueWith(_ => _isInvulnerable = false);
    }

    private void UpdateEnrageTimer(float deltaTime)
    {
        // Only update enrage timer if not already enraged and not defeated
        if (_isEnraged || IsDefeated)
            return;

        _enrageTimer += deltaTime;

        // Check for enrage threshold
        if (_enrageTimer >= _enrageThreshold)
        {
            //EnterEnrageMode();
        }
        // Optional: Warn player when enrage is approaching
        else if (_enrageTimer >= _enrageThreshold - 30f) // 30 seconds warning
        {
            float warningIntensity = (_enrageTimer - (_enrageThreshold - 30f)) / 30f;
            ShowEnrageWarning(warningIntensity);
        }
    }

    private void ShowEnrageWarning(float intensity)
    {
        // Visual warning effects that scale with intensity (0-1)
        Color warningColor = Color.Red * (0.3f * intensity);
        // TODO: Add visual warning effects

        // Audio warning if intensity crosses thresholds
        //if (intensity >= 0.25f && _lastWarningThreshold < 0.25f)
        //    PlayWarningSound(0.25f);
        //else if (intensity >= 0.5f && _lastWarningThreshold < 0.5f)
        //    PlayWarningSound(0.5f);
        //else if (intensity >= 0.75f && _lastWarningThreshold < 0.75f)
        //    PlayWarningSound(0.75f);

        _lastWarningThreshold = intensity;
    }

    //private void EnterEnrageMode()
    //{
    //    if (_isEnraged)
    //        return;

    //    _isEnraged = true;

    //    // Boost boss stats
    //    AttackPower = (int)(AttackPower * 1.5f);
    //    MovementComponent.SetMovementSpeed(1.2f);

    //    // Reduce ability cooldowns
    //    foreach (var ability in _phaseAbilities)
    //    {
    //        ability.Cooldown *= 0.7f;
    //    }

    //    // Visual and audio effects for enrage
    //    Flash();
    //    // _enrageSound?.Play();

    //    // Trigger any enrage-specific abilities or behaviors
    //    _currentPhase?.OnEnrage();
    //}

    // Add field for tracking warning threshold
    private float _lastWarningThreshold = 0f;

    private void PlayPhaseTransition()
    {
        // Set invulnerability during transition
        _isInvulnerable = true;

        // Create visual effects
        Flash();

        // Play different effects based on the phase we're transitioning to
        switch (_phaseIndex)
        {
            case 1: // Transitioning to Combat Phase
                    // Play combat start effects
                PlayPhaseTransitionEffects("combat_start");
                break;

            case 2: // Transitioning to Transition Phase
                    // Play transformation effects
                PlayPhaseTransitionEffects("transform");
                break;

            case 3: // Transitioning to Enraged Phase
                    // Play enrage effects
                PlayPhaseTransitionEffects("enrage");
                // Apply enrage buffs
                //EnterEnrageMode();
                break;
        }

        // Reset all abilities for the new phase
        //ResetPhaseAbilities();

        // Reset invulnerability after transition animation (1.5 seconds)
        Task.Delay(TimeSpan.FromSeconds(1.5f))
            .ContinueWith(_ =>
            {
                _isInvulnerable = false;
                // Grant a brief window of player counterattack opportunity
                _globalCooldown = 1.0f;
            });
    }

    //private void ResetPhaseAbilities()
    //{
    //    // Clear all current cooldowns
    //    _abilityCooldowns.Clear();
    //    _globalCooldown = 0f;

    //    // Get abilities for the current phase
    //    var phaseAbilities = _currentPhase switch
    //    {
    //        CombatPhase combatPhase => combatPhase._phaseAbilities,
    //        EnragedPhase enragedPhase => enragedPhase._phaseAbilities,
    //        _ => new List<SpecialAbility>()
    //    };

    //    // Update the active abilities for this phase
    //    _phaseAbilities = phaseAbilities;

    //    // Apply phase-specific modifiers
    //    foreach (var ability in _phaseAbilities)
    //    {
    //        // Adjust cooldowns based on phase
    //        //if (_currentPhase is EnragedPhase)
    //        //{
    //        //    ability.Cooldown *= 0.7f; // 30% faster cooldowns in enraged phase
    //        //}
    //        //else
    //        //{
    //        //    // Reset to base cooldown values
    //        //    ability.Cooldown = ability.BaseCooldown;
    //        //}

    //        //// Pre-start some abilities on cooldown to prevent immediate use
    //        //if (ability.StartOnCooldown)
    //        //{
    //        //    _abilityCooldowns[ability.Id] = ability.Cooldown * 0.5f; // Start at half cooldown
    //        //}
    //    }

    //    // Clear any existing hazard areas
    //    _hazardAreas.Clear();

    //    // Reset any phase-specific stats or states
    //    if (_currentPhase is TransitionPhase)
    //    {
    //        // Ensure complete invulnerability during transition
    //        _isInvulnerable = true;
    //    }
    //}
}