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

    [Header("Warning Icon & Attack UI Settings")]
    [SerializeField] private bool showAttackButton = true;
    [SerializeField] private GameObject warningIconPrefab;
    [SerializeField] private float warningIconHeightOffset = 3f;

    [Header("Exit Play Mode Settings")]
    [Tooltip("Khi tích chọn, nếu tất cả công trình/tháp bị phá hủy thì game sẽ tự động thoát chế độ Play.")]
    [SerializeField] private bool exitPlayModeWhenNoBuildings = false;

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

    private System.Collections.IEnumerator WaveSpawnRoutine()
    {
        float interval = waveInterval > 0f ? waveInterval : 5f;
        while (true)
        {
            SpawnEnemy();
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

        List<GameObject> spawnedWaveEnemies = new List<GameObject>();

        // Now, at each source position, spawn either a grid or a single enemy
        foreach (Transform source in sources)
        {
            List<EnemyAI> squadList = new List<EnemyAI>();

            if (useGridSpawn)
            {
                SpawnGridAt(source.position, source.rotation, squadList, spawnedWaveEnemies);
            }
            else
            {
                SpawnAtPosition(source.position, source.rotation, squadList, spawnedWaveEnemies);
            }
        }

        // Spawn Warning Attack Button on Lead Enemy of the wave
        if (showAttackButton && spawnedWaveEnemies.Count > 0)
        {
            Transform leadEnemy = spawnedWaveEnemies[0].transform;
            if (warningIconPrefab != null)
            {
                GameObject warningObj = Instantiate(warningIconPrefab, leadEnemy.position + Vector3.up * warningIconHeightOffset, Quaternion.identity);
                UIEnemyWaveButton btn = warningObj.GetComponent<UIEnemyWaveButton>();
                if (btn == null) btn = warningObj.AddComponent<UIEnemyWaveButton>();
                btn.Initialize(leadEnemy, warningIconHeightOffset);
            }
            else
            {
                UIEnemyWaveButton.CreateButton(leadEnemy, warningIconHeightOffset);
            }
        }
    }

    private void SpawnGridAt(Vector3 center, Quaternion rotation, List<EnemyAI> squadList, List<GameObject> spawnedWaveEnemies = null)
    {
        Vector3 forward = rotation * Vector3.forward;
        Vector3 right = rotation * Vector3.right;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                // Calculate offset from center of grid
                float offsetX = (c - (cols - 1) * 0.5f) * spacingX;
                float offsetZ = (r - (rows - 1) * 0.5f) * spacingZ;

                Vector3 spawnPos = center + right * offsetX + forward * offsetZ;
                SpawnAtPosition(spawnPos, rotation, squadList, spawnedWaveEnemies);
            }
        }
    }

    private GameObject SpawnAtPosition(Vector3 position, Quaternion rotation, List<EnemyAI> squadList, List<GameObject> spawnedWaveEnemies = null)
    {
        Debug.Log($"[EnemySpawn] Spawning enemy at position: {position}");
        GameObject enemy = Instantiate(enemyPrefab, position, rotation);
        if (spawnedWaveEnemies != null) spawnedWaveEnemies.Add(enemy);

        // Assign attack target to EnemyAI if it exists
        EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.exitPlayModeWhenNoBuildings = exitPlayModeWhenNoBuildings;

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
        return enemy;
    }
}
