using UnityEngine;
using UnityEngine.UI;

/*
 * TutorialSceneScanner.cs
 * CHỨC NĂNG: Quét Scene tìm vị trí công trình & Bốt địch & UI con bên trong công trình
 */
public class TutorialSceneScanner : MonoBehaviour
{
    public static TutorialSceneScanner Ins { get; private set; }

    [Header("=== TAGS CONFIG ===")]
    [SerializeField] private string buildingTag = "Building";
    [SerializeField] private string enemySpawnTag = "EnemySpawnPoint";

    private void Awake()
    {
        if (Ins == null) Ins = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 1. Tìm công trình cụ thể đã được người chơi đặt trên Scene
    /// </summary>
    public UpgradeableBuilding FindPlacedBuilding(BuildingType type)
    {
        UpgradeableBuilding[] buildings = FindObjectsByType<UpgradeableBuilding>(FindObjectsSortMode.None);
        
        foreach (var b in buildings)
        {
            if (b.buildingType == type)
            {
                return b;
            }
        }

        Debug.LogWarning($"[TUTORIAL SCANNER] Không tìm thấy công trình loại: {type} trên Scene!");
        return null;
    }

    /// <summary>
    /// 2. Tìm Script quản lý UI (BuildingUpgradeUI) nằm trên công trình hoặc object con
    /// </summary>
    public BuildingUpgradeUI GetBuildingUI(UpgradeableBuilding building)
    {
        if (building == null) return null;
        // Bật true để quét cả các object con UI đang bị ẨN (Inactive)
        return building.GetComponentInChildren<BuildingUpgradeUI>(true);
    }

    /// <summary>
    /// 3. Kiểm tra xem Bảng UI nâng cấp của công trình đó đang BẬT hay TẮT
    /// </summary>
    public bool IsBuildingUIOpen(UpgradeableBuilding building)
    {
        BuildingUpgradeUI ui = GetBuildingUI(building);
        return ui != null && ui.IsOpen;
    }

    /// <summary>
    /// 4. Quét sâu vào trong UI con để lấy đúng RectTransform của Nút Nâng Cấp
    /// </summary>
    public RectTransform GetUpgradeButtonTransform(UpgradeableBuilding building)
    {
        BuildingUpgradeUI ui = GetBuildingUI(building);
        if (ui != null && ui.UpgradeButton != null)
        {
            return ui.UpgradeButton.GetComponent<RectTransform>();
        }

        Debug.LogWarning($"[TUTORIAL SCANNER] Không tìm thấy UpgradeButton trên công trình: {building.buildingName}");
        return null;
    }

    /// <summary>
    /// 5. Tìm vị trí Trại/Căn cứ lính địch
    /// </summary>
    public Transform GetEnemyCampTransform()
    {
        GameObject spawnObj = GameObject.FindGameObjectWithTag(enemySpawnTag);
        if (spawnObj != null)
        {
            return spawnObj.transform;
        }

        Debug.LogWarning("[TUTORIAL SCANNER] Chưa gán Tag 'EnemySpawnPoint' cho điểm spawn địch!");
        return null;
    }
}