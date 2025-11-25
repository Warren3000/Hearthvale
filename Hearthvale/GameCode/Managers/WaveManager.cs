using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Hearthvale.GameCode.Data;
using Hearthvale.GameCode.Entities.Players;
using Microsoft.Xna.Framework;

namespace Hearthvale.GameCode.Managers
{
    public class WaveManager
    {
        private readonly NpcManager _npcManager;
        private readonly Player _player;
        private WaveConfig _config;
        
        private int _currentWaveIndex = -1;
        private int _currentGroupIndex = 0;
        private int _enemiesSpawnedInGroup = 0;
        
        private float _timer = 0f;
        private bool _isWaveActive = false;
        private bool _isWaitingForNextWave = false;
        
        // Track spawned enemies to know when wave is cleared
        private List<Hearthvale.GameCode.Entities.NPCs.NPC> _currentWaveEnemies = new List<Hearthvale.GameCode.Entities.NPCs.NPC>();

        public int CurrentWaveNumber => _currentWaveIndex + 1;
        public bool IsWaveActive => _isWaveActive;

        public WaveManager(NpcManager npcManager, Player player)
        {
            _npcManager = npcManager;
            _player = player;
        }

        public void LoadConfig(string path)
        {
            if (!File.Exists(path))
            {
                System.Diagnostics.Debug.WriteLine($"Wave config file not found: {path}");
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                _config = JsonSerializer.Deserialize<WaveConfig>(json, options);
                StartNextWave();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading wave config: {ex.Message}");
            }
        }

        public void Update(GameTime gameTime)
        {
            if (_config == null || _currentWaveIndex >= _config.Waves.Count) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_isWaitingForNextWave)
            {
                _timer -= dt;
                if (_timer <= 0)
                {
                    _isWaitingForNextWave = false;
                    _isWaveActive = true;
                    _currentGroupIndex = 0;
                    _enemiesSpawnedInGroup = 0;
                    _timer = 0; // Reset timer for first spawn
                    System.Diagnostics.Debug.WriteLine($"Starting Wave {CurrentWaveNumber}");
                }
                return;
            }

            if (_isWaveActive)
            {
                // Check if we are done spawning everything for this wave
                var currentWave = _config.Waves[_currentWaveIndex];
                
                // Clean up defeated enemies from tracking list
                _currentWaveEnemies.RemoveAll(e => e.IsDefeated || e.IsReadyToRemove);

                if (_currentGroupIndex >= currentWave.Groups.Count)
                {
                    // All groups spawned. Check if all enemies are dead.
                    if (_currentWaveEnemies.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Wave {CurrentWaveNumber} Cleared!");
                        _isWaveActive = false;
                        StartNextWave();
                    }
                    return;
                }

                // Spawning logic
                _timer -= dt;
                if (_timer <= 0)
                {
                    SpawnNextEnemy();
                }
            }
        }

        private void StartNextWave()
        {
            _currentWaveIndex++;
            if (_currentWaveIndex < _config.Waves.Count)
            {
                _isWaitingForNextWave = true;
                _timer = _config.Waves[_currentWaveIndex].SpawnDelay;
                System.Diagnostics.Debug.WriteLine($"Next wave in {_timer} seconds...");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("All waves completed!");
            }
        }

        private void SpawnNextEnemy()
        {
            var currentWave = _config.Waves[_currentWaveIndex];
            var currentGroup = currentWave.Groups[_currentGroupIndex];

            // Spawn enemy at screen edge (approx 200-300 units away)
            // Assuming 3.0 zoom on 1080p gives ~360 height, so 200 is closer to player but still reasonable distance
            var npc = _npcManager.SpawnNpcAroundPlayer(currentGroup.EnemyType, _player, 200f, 300f);
            if (npc != null)
            {
                _currentWaveEnemies.Add(npc);
            }
            
            _enemiesSpawnedInGroup++;

            // Set timer for next spawn
            _timer = currentGroup.SpawnInterval;

            // Check if group is finished
            if (_enemiesSpawnedInGroup >= currentGroup.Count)
            {
                _currentGroupIndex++;
                _enemiesSpawnedInGroup = 0;
            }
        }
    }
}