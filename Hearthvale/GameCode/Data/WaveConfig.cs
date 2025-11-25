using System.Collections.Generic;

namespace Hearthvale.GameCode.Data
{
    public class WaveConfig
    {
        public List<WaveData> Waves { get; set; }
    }

    public class WaveData
    {
        public int WaveNumber { get; set; }
        public float SpawnDelay { get; set; } // Delay before this wave starts spawning
        public List<WaveGroup> Groups { get; set; }
    }

    public class WaveGroup
    {
        public string EnemyType { get; set; }
        public int Count { get; set; }
        public float SpawnInterval { get; set; } // Time between spawns in this group
    }
}