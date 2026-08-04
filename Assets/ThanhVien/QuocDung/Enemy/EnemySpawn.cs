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
    [SerializeField] private float waveInterval = 5f;
    [Tooltip("Tự động spawn Enemy theo chu kỳ Wave độc lập, giúp Enemy luôn spawn ngay cả khi TẮT/BẬT Tutorial")]
    [SerializeField] private bool autoSpawnWaveAlways = true;

    [Header("Warning Icon & Attack UI Settings")]
    [SerializeField] private bool showAttackButton = true;
    [SerializeField] private GameObject warningIconPrefab;
    [SerializeField] private float warningIconHeightOffset = 3f;

    [Header("Cài Đặt Kích Thước Mũi Tên & Cảnh Báo (Spawn Warning Arrow)")]
    [Tooltip("Điều chỉnh chiều rộng mũi tên dưới chân Enemy")]
    [Range(0.1f, 5f)] public float warningArrowSize = 1.0f;

    [Tooltip("Điều chỉnh độ dài kéo dài của mũi tên (1.0 = duỗi đúng tới mục tiêu, >1.0 = dài hơn, <1.0 = ngắn hơn)")]
    [Range(0.1f, 5f)] public float warningArrowLengthMultiplier = 1.0f;

    [Tooltip("Độ dài cộng thêm cố định (mét) cho mũi tên")]
    public float warningArrowExtraLength = 0.0f;

    [Tooltip("Điều chỉnh kích thước chữ đếm ngược")]
    [Range(0.1f, 5f)] public float warningTimerTextScale = 1.0f;

    [Tooltip("Độ cao chữ đếm ngược trên đầu/thân Enemy")]
    [Range(0.5f, 5f)] public float warningTextHeightOffset = 1.8f;

    [Header("Exit Play Mode Settings")]
    [Tooltip("Khi tích chọn, nếu tất cả công trình/tháp bị phá hủy thì game sẽ tự động thoát chế độ Play.")]
    [SerializeField] private bool exitPlayModeWhenNoBuildings = false;

    private Coroutine waveSpawnCoroutine;

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnEnemy();
        }

        // Đảm bảo hệ thống spawn sóng luôn hoạt động khi game chạy (cho dù bật hay tắt Tutorial)
        if (autoSpawnWaveAlways || useWaveSpawn)
        {
            StartWaveSpawning();
        }
    }

    private void Update()
    {
        bool shouldRunWaves = useWaveSpawn || autoSpawnWaveAlways;
        if (shouldRunWaves)
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

        // Gắn Mũi Tên & Cảnh Báo cho con Thủ Lĩnh (Lead Enemy)
        if (showAttackButton && spawnedWaveEnemies.Count > 0)
        {
            Transform leadEnemy = spawnedWaveEnemies[0].transform;
            EnemySpawnWarningArrow arrow = EnemySpawnWarningArrow.Create(leadEnemy);
            if (arrow != null)
            {
                arrow.arrowSize = warningArrowSize;
                arrow.arrowLengthMultiplier = warningArrowLengthMultiplier;
                arrow.arrowExtraLength = warningArrowExtraLength;
                arrow.timerTextScale = warningTimerTextScale;
                arrow.textHeightOffset = warningTextHeightOffset;
                arrow.UpdateVisuals();
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

        EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.exitPlayModeWhenNoBuildings = exitPlayModeWhenNoBuildings;

            if (attackTarget != null)
            {
                enemyAI.villageCenter = attackTarget;
            }

            if (squadList != null)
            {
                squadList.Add(enemyAI);
                enemyAI.squadEnemies = squadList;
            }
        }
        return enemy;
    }
}
