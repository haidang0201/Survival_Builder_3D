using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/*
 * TutorialSceneScanner.cs
 * CHỨC NĂNG: Quét Scene tìm vị trí công trình & Bốt địch & UI con bên trong công trình
 */
public class TutorialSceneScanner : MonoBehaviour
{
    public static TutorialSceneScanner Ins { get; private set; }

    [Header("=== TAGS CONFIG ===")]
    [HideInInspector] public string buildingTag = "Building";
    [HideInInspector] public string enemySpawnTag = "EnemySpawnPoint";

    [Header("=== KẾT QUẢ QUÉT (SCAN RESULTS) ===")]
    [Tooltip("Danh sách các công trình đã được hệ thống tự động quét tìm thấy")]
    [SerializeField] private List<UpgradeableBuilding> scannedBuildings = new List<UpgradeableBuilding>();

    [Tooltip("Danh sách các bốt/điểm spawn địch đã quét được")]
    [SerializeField] private List<GameObject> scannedEnemySpawnPoints = new List<GameObject>();

    private void Awake()
    {
        if (Ins == null) Ins = this;
        else Destroy(gameObject);

        ScanScene();
    }

    /// <summary>
    /// Tự động quét toàn bộ Scene để tìm Công trình & Điểm Spawn Địch
    /// </summary>
    [ContextMenu("🔍 Quét Scene Ngay")]
    public void ScanScene()
    {
        scannedBuildings.Clear();
        scannedEnemySpawnPoints.Clear();

        // 1. Quét công trình theo Tag đã chọn
        GameObject[] buildingObjs = GameObject.FindGameObjectsWithTag(buildingTag);
        foreach (var obj in buildingObjs)
        {
            UpgradeableBuilding b = obj.GetComponent<UpgradeableBuilding>();
            if (b != null && !scannedBuildings.Contains(b))
            {
                scannedBuildings.Add(b);
            }
        }

        // Fallback: Nếu quét theo Tag chưa thấy thì quét theo Type
        if (scannedBuildings.Count == 0)
        {
            UpgradeableBuilding[] buildings = FindObjectsByType<UpgradeableBuilding>(FindObjectsSortMode.None);
            scannedBuildings.AddRange(buildings);
        }

        // 2. Quét điểm spawn địch theo Tag đã chọn
        GameObject[] enemyObjs = GameObject.FindGameObjectsWithTag(enemySpawnTag);
        scannedEnemySpawnPoints.AddRange(enemyObjs);

        Debug.Log($"[TUTORIAL SCANNER] Đã quét thành công: {scannedBuildings.Count} Công trình, {scannedEnemySpawnPoints.Count} Điểm Spawn Địch.");
    }

    /// <summary>
    /// 1. Tìm công trình cụ thể đã được người chơi đặt trên Scene từ danh sách quét
    /// </summary>
    public UpgradeableBuilding FindPlacedBuilding(BuildingType type)
    {
        foreach (var b in scannedBuildings)
        {
            if (b != null && b.buildingType == type) return b;
        }

        // Fallback quét trực tiếp nếu chưa có trong danh sách
        UpgradeableBuilding[] buildings = FindObjectsByType<UpgradeableBuilding>(FindObjectsSortMode.None);
        foreach (var b in buildings)
        {
            if (b != null && b.buildingType == type) return b;
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

        Debug.LogWarning($"[TUTORIAL SCANNER] Không tìm thấy UpgradeButton trên công trình: {(building != null ? building.buildingName : "null")}");
        return null;
    }

    /// <summary>
    /// 5. Tìm vị trí Trại/Căn cứ lính địch
    /// </summary>
    public Transform GetEnemyCampTransform()
    {
        if (scannedEnemySpawnPoints.Count > 0 && scannedEnemySpawnPoints[0] != null)
        {
            return scannedEnemySpawnPoints[0].transform;
        }

        GameObject spawnObj = GameObject.FindGameObjectWithTag(enemySpawnTag);
        if (spawnObj != null)
        {
            return spawnObj.transform;
        }

        Debug.LogWarning("[TUTORIAL SCANNER] Chưa gán Tag 'EnemySpawnPoint' cho điểm spawn địch!");
        return null;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TutorialSceneScanner))]
public class TutorialSceneScannerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TutorialSceneScanner scanner = (TutorialSceneScanner)target;

        serializedObject.Update();

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("🏷️ CHỌN TAG QUÉT CÔNG TRÌNH & ĐỊCH", EditorStyles.boldLabel);
        
        // Hiển thị Menu Dropdown chọn Tag tiêu chuẩn của Unity
        scanner.buildingTag = EditorGUILayout.TagField("Building Tag", scanner.buildingTag);
        scanner.enemySpawnTag = EditorGUILayout.TagField("Enemy Spawn Tag", scanner.enemySpawnTag);

        EditorGUILayout.Space(10);
        if (GUILayout.Button("🔍 QUÉT SCENE NGAY (SCAN SCENE)", GUILayout.Height(32)))
        {
            scanner.ScanScene();
            EditorUtility.SetDirty(scanner);
        }

        EditorGUILayout.Space(10);
        DrawDefaultInspector();

        serializedObject.ApplyModifiedProperties();
    }
}
#endif