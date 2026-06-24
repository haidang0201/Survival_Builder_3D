using UnityEngine;
using System.Collections.Generic;

public class SpawnSoldier : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject soldierPrefab;
    [SerializeField] private float spawnRadius = 5f;

    [Header("Upgrade Settings")]
    [SerializeField] private int currentLevel = 1;

    // Danh sách lưu các lính đã spawn để có thể xóa khi nâng cấp
    private List<GameObject> spawnedSoldiers = new List<GameObject>();
    private UpgradeableBuilding upgradeableBuilding;
    private bool isOnMainBuildingObject = false;

    void Awake()
    {
        // Tự động tìm component UpgradeableBuilding trên cùng Object hoặc ở Object cha
        upgradeableBuilding = GetComponent<UpgradeableBuilding>();
        if (upgradeableBuilding != null)
        {
            isOnMainBuildingObject = true;
        }
        else
        {
            upgradeableBuilding = GetComponentInParent<UpgradeableBuilding>();
            isOnMainBuildingObject = false;
        }
    }

    void OnEnable()
    {
        if (upgradeableBuilding != null)
        {
            // Nếu script này nằm trên Object cha cùng với UpgradeableBuilding
            if (isOnMainBuildingObject)
            {
                currentLevel = upgradeableBuilding.CurrentLevel + 1;
            }
            // Nếu nằm trên các model con của từng cấp độ
            else
            {
                int activeLevel = upgradeableBuilding.CurrentLevel + 1;
                // Chỉ sinh lính nếu cấp độ của script này trùng khớp với cấp độ thực tế của công trình
                if (currentLevel != activeLevel)
                {
                    return;
                }
            }
        }

        // Spawn số lượng lính tương ứng với Level hiện tại
        int initialCount = GetMaxSoldiersForLevel(currentLevel);
        SpawnSoldiers(initialCount);
    }

    void OnDisable()
    {
        // Khi Spawner bị tắt (do nâng cấp tắt model con hoặc bị hủy), xóa toàn bộ lính cũ
        // Chỉ dọn dẹp nếu danh sách thực sự có lính để tránh việc các model cấp độ khác bị tắt kích hoạt tính năng quét diện rộng phá hủy lính của model chính
        if (spawnedSoldiers != null && spawnedSoldiers.Count > 0)
        {
            ClearSpawnedSoldiers();
        }
    }

    void Update()
    {
        // Chỉ tự động đồng bộ hóa liên tục nếu script này nằm trên GameObject cha cùng với UpgradeableBuilding
        if (upgradeableBuilding != null && isOnMainBuildingObject)
        {
            int targetLevel = upgradeableBuilding.CurrentLevel + 1;
            if (currentLevel != targetLevel)
            {
                Debug.Log($"[SpawnSoldier] Đồng bộ nâng cấp: Level thay đổi từ {currentLevel} -> {targetLevel}. Tiến hành xóa lính cũ và spawn lính mới.");
                ClearSpawnedSoldiers();
                currentLevel = targetLevel;
                int newCount = GetMaxSoldiersForLevel(currentLevel);
                SpawnSoldiers(newCount);
            }
        }
    }

    // Hàm lấy số lượng lính tối đa dựa theo Level
    public int GetMaxSoldiersForLevel(int level)
    {
        switch (level)
        {
            case 1: return 4;
            case 2: return 6;
            case 3: return 7;
            default: return 4; // Fallback
        }
    }

    // Hàm lấy sát thương của lính dựa theo Level
    public float GetDamageForLevel(int level)
    {
        switch (level)
        {
            case 1: return 10f;
            case 2: return 20f;
            case 3: return 50f;
            default: return 10f; // Fallback
        }
    }

    // Hàm dùng để spawn một số lượng lính nhất định
    public void SpawnSoldiers(int count)
    {
        if (soldierPrefab == null)
        {
            Debug.LogWarning("Soldier Prefab chưa được gán trong Inspector!");
            return;
        }

        float damage = GetDamageForLevel(currentLevel);
        Debug.Log($"[SpawnSoldier] {gameObject.name} (Lv {currentLevel}) đang spawn {count} lính mới với sát thương {damage}.");

        for (int i = 0; i < count; i++)
        {
            // Tính toán vị trí ngẫu nhiên trên mặt phẳng XZ xung quanh Spawner
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPosition = new Vector3(
                transform.position.x + randomCircle.x,
                transform.position.y,
                transform.position.z + randomCircle.y
            );

            // Spawn Soldier
            GameObject soldier = Instantiate(soldierPrefab, spawnPosition, Quaternion.identity);

            // Thiết lập sát thương cho lính mới spawn
            UnitController unit = soldier.GetComponent<UnitController>();
            if (unit != null)
            {
                unit.SetAttackDamage(damage);
            }

            // Lưu vào danh sách
            spawnedSoldiers.Add(soldier);
        }
    }

    // Hàm dọn dẹp các lính cũ đang hoạt động
    private void ClearSpawnedSoldiers()
    {
        Debug.Log($"[SpawnSoldier] ClearSpawnedSoldiers được gọi trên {gameObject.name}. Danh sách đang có {spawnedSoldiers.Count} lính.");

        // 1. Xóa lính trong danh sách theo dõi
        foreach (GameObject soldier in spawnedSoldiers)
        {
            if (soldier != null)
            {
                Debug.Log($"[SpawnSoldier] Hủy lính trong danh sách: {soldier.name}");
                Destroy(soldier);
            }
        }
        spawnedSoldiers.Clear();

        // 2. Quét diện rộng xung quanh Spawner để dọn sạch lính cũ lọt lưới (đề phòng lỗi đồng bộ)
        Collider[] colliders = Physics.OverlapSphere(transform.position, spawnRadius * 3f);
        foreach (var col in colliders)
        {
            if (col != null)
            {
                UnitController unit = col.GetComponentInParent<UnitController>();
                if (unit != null && unit.gameObject != gameObject)
                {
                    Debug.Log($"[SpawnSoldier] Phát hiện và hủy lính lọt lưới xung quanh: {unit.gameObject.name}");
                    Destroy(unit.gameObject);
                }
            }
        }
    }

    // Hàm public dùng để nâng cấp thủ công (nếu không dùng UpgradeableBuilding)
    [ContextMenu("Upgrade")]
    public void Upgrade()
    {
        if (currentLevel >= 3)
        {
            Debug.Log("Đã đạt cấp độ tối đa (Level 3)!");
            return;
        }

        // 1. Xóa các lính của level trước
        ClearSpawnedSoldiers();

        // 2. Nâng cấp level
        currentLevel++;

        // 3. Spawn số lượng lính của level mới
        int newMax = GetMaxSoldiersForLevel(currentLevel);
        SpawnSoldiers(newMax);

        Debug.Log($"Nâng cấp thành công lên Level {currentLevel}! Đã xóa lính cũ và spawn {newMax} lính mới với sát thương {GetDamageForLevel(currentLevel)}.");
    }

    // Property để đọc Level hiện tại từ các script khác
    public int CurrentLevel => currentLevel;
}
