using UnityEngine;
using System.Collections.Generic;

public class EnemySpawn : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Attack Target (Optional)")]
    [SerializeField] private Transform attackTarget;

    [Header("Grid Spawn Settings")]
    [SerializeField] private bool useGridSpawn = false;
    [SerializeField] private int rows = 1;
    [SerializeField] private int cols = 1;
    [SerializeField] private float spacingX = 2f;
    [SerializeField] private float spacingZ = 2f;

    [Header("Time & Wave Spawning Settings")]
    [SerializeField] private bool useWaveSpawn = true;
    [SerializeField] private bool spawnOnlyAtNight = true;
    [SerializeField] private float waveInterval = 5f;

    [Header("Day Scaling Settings (Difficulty)")]
    [SerializeField] private bool scaleDifficultyWithDays = true;
    [Tooltip("How many days it takes to increase the number of active spawn locations per wave by 1.")]
    [SerializeField] private int daysToIncreaseSpawnPoints = 2;
    [Tooltip("How many extra rows are added to the grid spawn per day.")]
    [SerializeField] private int extraRowsPerDay = 0;
    [Tooltip("How many extra columns are added to the grid spawn per day.")]
    [SerializeField] private int extraColsPerDay = 1;
    [Tooltip("How many extra enemies are spawned per spawn point per day (if not using grid spawn).")]
    [SerializeField] private int extraEnemiesPerDay = 1;
    [Tooltip("How much the wave interval (seconds) is reduced per day.")]
    [SerializeField] private float waveIntervalReductionPerDay = 0.5f;
    [Tooltip("The minimum wave interval limit.")]
    [SerializeField] private float minWaveInterval = 2f;

    [Header("Warning Icon Settings")]
    [SerializeField] private GameObject warningIconPrefab;
    [SerializeField] private float warningIconHeightOffset = 3f;

    private Coroutine waveSpawnCoroutine;

    private void Start()
    {
        if (spawnOnStart && !useWaveSpawn)
        {
            SpawnEnemy();
        }
    }

    private void Update()
    {
        if (!useWaveSpawn) return;

        if (DayNightManager.Ins == null) return;

        bool shouldSpawn = !spawnOnlyAtNight || DayNightManager.Ins.IsNight();

        if (shouldSpawn)
        {
            StartWaveSpawning();
        }
        else
        {
            StopWaveSpawning();
        }
    }

    private void OnDisable()
    {
        StopWaveSpawning();
    }

    private void StartWaveSpawning()
    {
        if (waveSpawnCoroutine == null)
        {
            waveSpawnCoroutine = StartCoroutine(WaveSpawnRoutine());
            Debug.Log("[EnemySpawn] Started wave spawning.");
        }
    }

    private void StopWaveSpawning()
    {
        if (waveSpawnCoroutine != null)
        {
            StopCoroutine(waveSpawnCoroutine);
            waveSpawnCoroutine = null;
            Debug.Log("[EnemySpawn] Stopped wave spawning.");
        }
    }

    private int currentSpawnPointIndex = 0;

    private System.Collections.IEnumerator WaveSpawnRoutine()
    {
        currentSpawnPointIndex = 0; // Reset index when starting wave spawning

        while (true)
        {
            int currentDay = DayNightManager.Ins != null ? DayNightManager.Ins.CurrentDay : 0;

            float interval = waveInterval;
            if (scaleDifficultyWithDays)
            {
                interval -= currentDay * waveIntervalReductionPerDay;
                interval = Mathf.Max(interval, minWaveInterval);
            }
            if (interval <= 0f) interval = 5f; // Fallback safety

            if (enemyPrefab != null)
            {
                List<Transform> sources = GetSpawnSources();
                if (sources.Count > 0)
                {
                    int numSpawnLocations = 1;
                    if (scaleDifficultyWithDays)
                    {
                        numSpawnLocations += currentDay / Mathf.Max(1, daysToIncreaseSpawnPoints);
                        numSpawnLocations = Mathf.Min(numSpawnLocations, sources.Count);
                    }

                    for (int i = 0; i < numSpawnLocations; i++)
                    {
                        int targetIndex = (currentSpawnPointIndex + i) % sources.Count;
                        Transform source = sources[targetIndex];
                        if (source != null)
                        {
                            Debug.Log($"[EnemySpawn] Spawning wave at point {targetIndex}: {source.name} (Day {currentDay})");
                            SpawnAtSourceWithScaling(source, currentDay);
                        }
                    }

                    currentSpawnPointIndex = (currentSpawnPointIndex + numSpawnLocations) % sources.Count;
                }
            }
            yield return new WaitForSeconds(interval);
        }
    }

    public void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("EnemySpawn: Enemy Prefab is not assigned!", this);
            return;
        }

        // Get all source positions where we want to spawn
        List<Transform> sources = GetSpawnSources();
        int currentDay = DayNightManager.Ins != null ? DayNightManager.Ins.CurrentDay : 0;

        // Now, at each source position, spawn either a grid or a single enemy with day scaling
        foreach (Transform source in sources)
        {
            if (source != null)
            {
                SpawnAtSourceWithScaling(source, currentDay);
            }
        }
    }

    private List<Transform> GetSpawnSources()
    {
        List<Transform> sources = new List<Transform>();
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            foreach (Transform t in spawnPoints)
            {
                if (t != null) sources.Add(t);
            }
        }
        else if (transform.childCount > 0)
        {
            foreach (Transform child in transform)
            {
                sources.Add(child);
            }
        }
        else
        {
            sources.Add(transform);
        }
        return sources;
    }

    private void SpawnAtSourceWithScaling(Transform source, int currentDay)
    {
        List<EnemyAI> squadList = new List<EnemyAI>();

        if (warningIconPrefab != null && source != null)
        {
            Vector3 warningPos = source.position + Vector3.up * warningIconHeightOffset;
            GameObject warningObj = Instantiate(warningIconPrefab, warningPos, Quaternion.identity);
            UIWarning uiWarning = warningObj.GetComponent<UIWarning>();
            if (uiWarning == null)
            {
                uiWarning = warningObj.AddComponent<UIWarning>();
            }
            uiWarning.Initialize(source.position);
        }

        if (useGridSpawn)
        {
            int targetRows = rows;
            int targetCols = cols;
            if (scaleDifficultyWithDays)
            {
                targetRows += currentDay * extraRowsPerDay;
                targetCols += currentDay * extraColsPerDay;
            }
            SpawnGridAt(source.position, source.rotation, targetRows, targetCols, squadList);
        }
        else
        {
            int spawnCount = 1;
            if (scaleDifficultyWithDays)
            {
                spawnCount += currentDay * extraEnemiesPerDay;
            }
            for (int i = 0; i < spawnCount; i++)
            {
                Vector3 offset = Vector3.zero;
                if (i > 0)
                {
                    Vector2 randomCircle = Random.insideUnitCircle * 1.5f;
                    offset = new Vector3(randomCircle.x, 0, randomCircle.y);
                }
                SpawnAtPosition(source.position + offset, source.rotation, squadList);
            }
        }
    }

    private void SpawnGridAt(Vector3 center, Quaternion rotation, int targetRows, int targetCols, List<EnemyAI> squadList)
    {
        Vector3 forward = rotation * Vector3.forward;
        Vector3 right = rotation * Vector3.right;

        for (int r = 0; r < targetRows; r++)
        {
            for (int c = 0; c < targetCols; c++)
            {
                // Calculate offset from center of grid
                float offsetX = (c - (targetCols - 1) * 0.5f) * spacingX;
                float offsetZ = (r - (targetRows - 1) * 0.5f) * spacingZ;

                Vector3 spawnPos = center + right * offsetX + forward * offsetZ;
                SpawnAtPosition(spawnPos, rotation, squadList);
            }
        }
    }

    private void SpawnAtPosition(Vector3 position, Quaternion rotation, List<EnemyAI> squadList)
    {
        Debug.Log($"[EnemySpawn] Spawning enemy at position: {position}");
        GameObject enemy = Instantiate(enemyPrefab, position, rotation);

        // Assign attack target to EnemyAI if it exists
        EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            if (attackTarget != null)
            {
                enemyAI.villageCenter = attackTarget;
            }

            // Assign squad list
            if (squadList != null)
            {
                squadList.Add(enemyAI);
                enemyAI.squadEnemies = squadList;
            }
        }
    }
}
