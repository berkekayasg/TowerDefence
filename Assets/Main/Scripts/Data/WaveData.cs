using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "WaveData", menuName = "TowerDefense/WaveData", order = 1)]
public class WaveData : ScriptableObject
{
    [System.Serializable]
    public struct EnemySpawnGroup // Renamed for clarity
    {
        public EnemyData enemyData;
        public int spawnCount;
        [Tooltip("Time delay between spawning each enemy in this group.")]
        public float timeBetweenSpawns; // Delay between individual enemies in this group
        [Tooltip("Time delay AFTER this group finishes spawning before the next group starts.")]
        public float delayAfterGroup; // Delay after this group finishes
    }

    [Tooltip("Optional delay before the first enemy of this wave spawns.")]
    public float initialWaveDelay = 0f;
    public List<EnemySpawnGroup> enemySpawnGroups; // Renamed list
}
