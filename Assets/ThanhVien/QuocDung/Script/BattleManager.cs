using UnityEngine;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    [Header("Spawn Locations")]
    [Tooltip("Vị trí sinh phe Người chơi (BÊN TRÁI)")]
    [SerializeField] private Transform leftSpawnPoint;
    [Tooltip("Vị trí sinh phe Enemy (BÊN PHẢI)")]
    [SerializeField] private Transform rightSpawnPoint;

    [Header("Distance & Grid Spacing Settings")]
    [SerializeField] private float buildingSpacing = 4.0f;
    [SerializeField] private float unitSpacing = 2.0f;
    [SerializeField] private int unitsPerRow = 4;

    [Header("Enemy Prefab Settings")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("Player Soldier Prefabs")]
    [SerializeField] private GameObject soldierPrefab;

    [Header("Player Building Prefabs")]
    [SerializeField] private GameObject barracksPrefab;
    [SerializeField] private GameObject archerTowerPrefab;
    [SerializeField] private GameObject watchTowerPrefab;
    [SerializeField] private GameObject cannonPrefab;

    [System.Serializable]
    public struct CustomBuildingPrefab
    {
        public BuildingType buildingType;
        public GameObject prefab;
    }

    [Header("Custom Building Mapping (Optional)")]
    [SerializeField] private List<CustomBuildingPrefab> customBuildingPrefabs = new List<CustomBuildingPrefab>();

    [Header("Standalone Test Mode (Kích hoạt khi mở trực tiếp BattleScene trong Editor)")]
    [SerializeField] private bool enableTestFallback = true;
    [SerializeField] private int testEnemyWaveCount = 1;
    [SerializeField] private int testBarracksCount = 1;
    [SerializeField] private int testBarracksLevel = 1;
    [SerializeField] private bool testSpawnArcherTower = true;

    [Header("Camera Settings")]
    [Tooltip("Camera chính dùng cho trận đấu (nếu chưa gán sẽ tự lấy Camera.main)")]
    [SerializeField] private Camera battleCamera;
    [Tooltip("Ô đế / Transform để gắn Camera tại vị trí giao tranh")]
    [SerializeField] private Transform battleCameraPoint;
    [Tooltip("Tự động di chuyển Camera đến vị trí giao tranh khi bắt đầu trận")]
    [SerializeField] private bool autoPositionCamera = true;
    [Tooltip("Độ lệch vị trí Camera so với trung tâm điểm giao tranh (nếu không dùng battleCameraPoint)")]
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 8f, -12f);
    [Tooltip("Góc xoay mặc định của Camera (nếu không dùng battleCameraPoint)")]
    [SerializeField] private Vector3 cameraRotation = new Vector3(30f, 0f, 0f);

    private List<GameObject> spawnedPlayerObjects = new List<GameObject>();
    private List<GameObject> spawnedEnemyObjects = new List<GameObject>();

    private void Awake()
    {
        Time.timeScale = 1f;
    }

    private void Start()
    {
        // 1. Kiểm tra vị trí Spawn mặc định nếu chưa gán trong Inspector
        EnsureSpawnPoints();

        // 2. Kiểm tra dữ liệu truyền từ Scene chính (qua BattleData)
        if (!BattleData.HasData && enableTestFallback)
        {
            SetupFallbackTestData();
        }

        // 3. Tiến hành Spawn theo yêu cầu:
        //    - Phe Người Chơi (Lính & Công trình) bên TRÁI
        //    - Phe Enemy (theo số lượng Wave) bên PHẢI
        SpawnPlayerSide();
        SpawnEnemySide();

        // 4. Thiết lập vị trí Camera tại giao tranh
        SetupBattleCamera();

        // 5. Cho lính và Enemy lập tức bay vào đánh nhau
        StartCoroutine(TriggerImmediateCombatRoutine());

        Debug.Log($"[BattleManager] 🔥 Trận đấu khởi tạo thành công! " +
                  $"Sinh {spawnedPlayerObjects.Count} vật thể Người Chơi (BÊN TRÁI) và {spawnedEnemyObjects.Count} Enemy (BÊN PHẢI).");
    }

    /// <summary>
    /// Kích hoạt cho cả Lính và Enemy lập tức xông vào đánh nhau khi mở Battle Scene
    /// </summary>
    private System.Collections.IEnumerator TriggerImmediateCombatRoutine()
    {
        yield return new WaitForEndOfFrame();

        Vector3 enemyTargetPos = (rightSpawnPoint != null) ? rightSpawnPoint.position : (transform.position + Vector3.right * 15f);

        // 1. Kích hoạt Lính người chơi lao vào đánh Enemy ở bên Phải
        UnitController[] playerUnits = Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None);
        foreach (var unit in playerUnits)
        {
            if (unit != null && unit.gameObject.activeInHierarchy)
            {
                unit.EnableCombat(enemyTargetPos);
            }
        }

        // 2. Kích hoạt Enemy lao vào đánh Lính người chơi ở bên Trái
        EnemyAI[] enemies = Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                enemy.EnableCombat();
            }
        }

        Debug.Log("[BattleManager] ⚔️ Cả lính và Enemy đã lập tức bay vào đánh nhau!");
    }

    /// <summary>
    /// Định vị Camera tại vị trí ô đế giao tranh (battleCameraPoint) hoặc điểm trung tâm trận đấu
    /// </summary>
    private void SetupBattleCamera()
    {
        if (battleCamera == null)
        {
            battleCamera = Camera.main;
        }

        if (battleCamera == null)
        {
            battleCamera = Object.FindFirstObjectByType<Camera>();
        }

        if (battleCamera == null || !autoPositionCamera) return;

        if (battleCameraPoint != null)
        {
            battleCamera.transform.position = battleCameraPoint.position;
            battleCamera.transform.rotation = battleCameraPoint.rotation;
            Debug.Log($"[BattleManager] Đã gắn Camera vào ô đế battleCameraPoint: {battleCameraPoint.position}");
        }
        else
        {
            // Tính vị trí trung tâm giữa phe Người chơi (Trái) và Enemy (Phải)
            Vector3 centerPos = (leftSpawnPoint.position + rightSpawnPoint.position) * 0.5f;
            battleCamera.transform.position = centerPos + cameraOffset;
            battleCamera.transform.rotation = Quaternion.Euler(cameraRotation);
            Debug.Log($"[BattleManager] Đã tự động di chuyển Camera đến tâm điểm giao tranh: {centerPos}");
        }
    }

    /// <summary>
    /// Đảm bảo tự tạo Spawn Point bên TRÁI và BÊN PHẢI nếu chưa gán trong Inspector
    /// </summary>
    private void EnsureSpawnPoints()
    {
        if (leftSpawnPoint == null)
        {
            GameObject leftObj = GameObject.Find("LeftSpawnPoint");
            if (leftObj != null)
            {
                leftSpawnPoint = leftObj.transform;
            }
            else
            {
                leftObj = new GameObject("LeftSpawnPoint_Player");
                leftObj.transform.position = transform.position + Vector3.left * 15f;
                leftSpawnPoint = leftObj.transform;
            }
        }

        if (rightSpawnPoint == null)
        {
            GameObject rightObj = GameObject.Find("RightSpawnPoint");
            if (rightObj != null)
            {
                rightSpawnPoint = rightObj.transform;
            }
            else
            {
                rightObj = new GameObject("RightSpawnPoint_Enemy");
                rightObj.transform.position = transform.position + Vector3.right * 15f;
                rightSpawnPoint = rightObj.transform;
            }
        }
    }

    /// <summary>
    /// Cài đặt dữ liệu giả lập cho chế độ Test độc lập
    /// </summary>
    private void SetupFallbackTestData()
    {
        BattleData.EnemyWaveCount = Mathf.Max(1, testEnemyWaveCount);
        BattleData.PlayerBuildings.Clear();

        // Tạo Doanh trại Test
        for (int i = 0; i < testBarracksCount; i++)
        {
            int lvl = Mathf.Clamp(testBarracksLevel, 1, 3);
            int soldiers = (lvl == 1) ? 4 : (lvl == 2 ? 6 : 8);

            BattleData.PlayerBuildings.Add(new BattleData.BuildingInfo
            {
                buildingType = BuildingType.BarracksMelee,
                level = lvl,
                soldierCount = soldiers,
                originalPosition = Vector3.zero
            });
        }

        // Tạo Tháp cung Test
        if (testSpawnArcherTower)
        {
            BattleData.PlayerBuildings.Add(new BattleData.BuildingInfo
            {
                buildingType = BuildingType.ArcherTower,
                level = 1,
                soldierCount = 0,
                originalPosition = Vector3.zero
            });
        }

        BattleData.HasData = true;
        Debug.Log("[BattleManager] Đã tự động tạo dữ liệu Test cho BattleScene.");
    }

    /// <summary>
    /// Spawn toàn bộ Công trình và Lính của Người Chơi ở BÊN TRÁI
    /// </summary>
    private void SpawnPlayerSide()
    {
        if (leftSpawnPoint == null) return;

        Vector3 originLeft = leftSpawnPoint.position;
        int buildingIndex = 0;
        int soldierTotalSpawned = 0;

        // Vị trí dòng lính đứng xếp hàng phía trước (quay mặt về phía bên phải)
        Vector3 soldierFrontOrigin = originLeft + Vector3.right * 5f;

        foreach (var buildingInfo in BattleData.PlayerBuildings)
        {
            // 1. Spawn Công trình
            GameObject buildingPrefab = GetBuildingPrefab(buildingInfo.buildingType);
            if (buildingPrefab != null)
            {
                Vector3 buildPos = originLeft + Vector3.left * (buildingIndex * buildingSpacing) + Vector3.back * (buildingIndex % 2 * 2f);
                Quaternion buildRot = Quaternion.Euler(0, 90, 0); // Quay mặt về phía bên phải (Enemy)

                GameObject spawnedBuilding = Instantiate(buildingPrefab, buildPos, buildRot);
                spawnedBuilding.name = $"Player_{buildingInfo.buildingType}_Lv{buildingInfo.level}";
                spawnedPlayerObjects.Add(spawnedBuilding);

                // Tắt SpawnSoldier trên công trình ở SceneBattle để tránh việc công trình tự động spawn lính lần 2!
                SpawnSoldier spawner = spawnedBuilding.GetComponent<SpawnSoldier>();
                if (spawner == null) spawner = spawnedBuilding.GetComponentInChildren<SpawnSoldier>();
                if (spawner != null)
                {
                    spawner.enabled = false;
                }

                // Cập nhật Cấp độ cho UpgradeableBuilding nếu có
                UpgradeableBuilding ub = spawnedBuilding.GetComponent<UpgradeableBuilding>();
                if (ub == null) ub = spawnedBuilding.GetComponentInChildren<UpgradeableBuilding>();
                if (ub != null)
                {
                    // Cài đặt level nếu cần
                }
            }

            // 2. Spawn Lính thuộc về Công trình này (hoặc tổng số lính)
            int countToSpawn = buildingInfo.soldierCount;
            for (int i = 0; i < countToSpawn; i++)
            {
                if (soldierPrefab != null)
                {
                    int row = soldierTotalSpawned / unitsPerRow;
                    int col = soldierTotalSpawned % unitsPerRow;

                    Vector3 soldierPos = soldierFrontOrigin + Vector3.left * (row * unitSpacing) + Vector3.forward * (col * unitSpacing - 1.5f);
                    Quaternion soldierRot = Quaternion.Euler(0, 90, 0); // Quay mặt về phía Enemy (Bên Phải)

                    GameObject spawnedSoldier = Instantiate(soldierPrefab, soldierPos, soldierRot);
                    spawnedSoldier.name = $"Player_Soldier_{soldierTotalSpawned + 1}";
                    spawnedPlayerObjects.Add(spawnedSoldier);
                }
                soldierTotalSpawned++;
            }

            buildingIndex++;
        }

        // Nếu tổng số lính đã spawn chưa đủ số lính thực tế trong căn cứ
        if (soldierTotalSpawned < BattleData.TotalSoldiersInBase)
        {
            int remaining = BattleData.TotalSoldiersInBase - soldierTotalSpawned;
            for (int i = 0; i < remaining; i++)
            {
                if (soldierPrefab != null)
                {
                    int idx = soldierTotalSpawned + i;
                    int row = idx / unitsPerRow;
                    int col = idx % unitsPerRow;

                    Vector3 soldierPos = soldierFrontOrigin + Vector3.left * (row * unitSpacing) + Vector3.forward * (col * unitSpacing - 1.5f);
                    Quaternion soldierRot = Quaternion.Euler(0, 90, 0);

                    GameObject spawnedSoldier = Instantiate(soldierPrefab, soldierPos, soldierRot);
                    spawnedSoldier.name = $"Player_Soldier_{idx + 1}";
                    spawnedPlayerObjects.Add(spawnedSoldier);
                }
            }
        }
    }

    /// <summary>
    /// Spawn toàn bộ Enemy thuộc Wave ở BÊN PHẢI
    /// </summary>
    private void SpawnEnemySide()
    {
        if (rightSpawnPoint == null || enemyPrefab == null)
        {
            Debug.LogWarning("[BattleManager] Chưa cài đặt rightSpawnPoint hoặc enemyPrefab!");
            return;
        }

        int count = Mathf.Max(1, BattleData.EnemyWaveCount);
        Vector3 originRight = rightSpawnPoint.position;

        for (int i = 0; i < count; i++)
        {
            int row = i / unitsPerRow;
            int col = i % unitsPerRow;

            Vector3 enemyPos = originRight + Vector3.right * (row * unitSpacing) + Vector3.forward * (col * unitSpacing - 1.5f);
            Quaternion enemyRot = Quaternion.Euler(0, -90, 0); // Quay mặt về phía bên Trái (Player)

            GameObject spawnedEnemy = Instantiate(enemyPrefab, enemyPos, enemyRot);
            spawnedEnemy.name = $"Enemy_WaveUnit_{i + 1}";
            spawnedEnemyObjects.Add(spawnedEnemy);

            // Kích hoạt AI giao tranh cho Enemy nếu có
            EnemyAI enemyAI = spawnedEnemy.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.EnableCombat();
            }
        }
    }

    /// <summary>
    /// Tìm Prefab công trình dựa theo BuildingType
    /// </summary>
    private GameObject GetBuildingPrefab(BuildingType type)
    {
        // 1. Kiểm tra mảng Custom mapping trong Inspector
        foreach (var custom in customBuildingPrefabs)
        {
            if (custom.buildingType == type && custom.prefab != null)
            {
                return custom.prefab;
            }
        }

        // 2. Mặc định theo từng nhóm loại nhà
        switch (type)
        {
            case BuildingType.BarracksMelee:
            case BuildingType.BarracksArcher:
            case BuildingType.BarracksSpear:
                return barracksPrefab != null ? barracksPrefab : archerTowerPrefab;

            case BuildingType.ArcherTower:
                return archerTowerPrefab != null ? archerTowerPrefab : barracksPrefab;

            case BuildingType.WatchTower:
                return watchTowerPrefab != null ? watchTowerPrefab : archerTowerPrefab;

            case BuildingType.Cannon:
                return cannonPrefab != null ? cannonPrefab : archerTowerPrefab;

            default:
                return barracksPrefab != null ? barracksPrefab : archerTowerPrefab;
        }
    }
}
