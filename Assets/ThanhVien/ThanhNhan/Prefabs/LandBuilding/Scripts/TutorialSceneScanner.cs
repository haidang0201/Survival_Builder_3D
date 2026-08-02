using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/*
 * TutorialSceneScanner.cs
 * CHỨC NĂNG: Quét Scene tìm vị trí Công trình, Bốt địch, UI con (Sửa chữa, Nâng cấp) 
 *            và Nút (+) Mở rộng đất để hỗ trợ Tutorial chỉ tay chính xác.
 */
public class TutorialSceneScanner : MonoBehaviour
{
    public static TutorialSceneScanner Ins { get; private set; }

    [Header("=== TAGS & LAYER CONFIG ===")]
    [HideInInspector] public string buildingTag = "Building";
    [HideInInspector] public string enemySpawnTag = "EnemySpawnPoint";
    [Tooltip("Layer dành cho UI (Default: UI)")]
    public LayerMask uiLayer;

    [Header("=== KẾT QUẢ QUÉT (SCAN RESULTS) ===")]
    [Tooltip("Danh sách các công trình đã được hệ thống tự động quét tìm thấy")]
    [SerializeField] private List<UpgradeableBuilding> scannedBuildings = new List<UpgradeableBuilding>();

    [Tooltip("Danh sách các bốt/điểm spawn địch đã quét được")]
    [SerializeField] private List<GameObject> scannedEnemySpawnPoints = new List<GameObject>();

    private void Awake()
    {
        if (Ins == null) Ins = this;
        else Destroy(gameObject);

        // Thiết lập Layer UI mặc định nếu chưa gán
        if (uiLayer == 0)
        {
            uiLayer = LayerMask.GetMask("UI");
        }

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

        // 1. Quét công trình theo Tag
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

        // 2. Quét điểm spawn địch theo Tag
        GameObject[] enemyObjs = GameObject.FindGameObjectsWithTag(enemySpawnTag);
        scannedEnemySpawnPoints.AddRange(enemyObjs);

        Debug.Log($"[TUTORIAL SCANNER] Đã quét thành công: {scannedBuildings.Count} Công trình, {scannedEnemySpawnPoints.Count} Điểm Spawn Địch.");
    }

    /// <summary>
    /// 1. Tìm công trình cụ thể đã đặt trên Scene (Tự động re-scan nếu cần)
    /// </summary>
    public UpgradeableBuilding FindPlacedBuilding(BuildingType type)
    {
        foreach (var b in scannedBuildings)
        {
            if (b != null && b.buildingType == type) return b;
        }

        // 🔥 Tự động quét lại Scene để cập nhật các công trình mới xây từ Stage 2
        ScanScene();

        foreach (var b in scannedBuildings)
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
    /// 3. Kiểm tra xem Bảng UI nâng cấp/sửa chữa của công trình đang BẬT hay TẮT
    /// </summary>
    public bool IsBuildingUIOpen(UpgradeableBuilding building)
    {
        BuildingUpgradeUI ui = GetBuildingUI(building);
        return ui != null && ui.IsOpen;
    }

    /// <summary>
    /// 🔥 4. QUÉT NÚT NÂNG CẤP (UPGRADE BUTTON)
    /// </summary>
    public RectTransform GetUpgradeButtonTransform(UpgradeableBuilding building)
    {
        BuildingUpgradeUI ui = GetBuildingUI(building);
        if (ui != null)
        {
            if (ui.UpgradeButton != null)
            {
                return ui.UpgradeButton.GetComponent<RectTransform>();
            }

            // Fallback: Quét tìm Button có tên chứa "Upgrade" trong UI con
            Button[] buttons = ui.GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                if (btn.name.ToLower().Contains("upgrade"))
                {
                    return btn.GetComponent<RectTransform>();
                }
            }
        }

        Debug.LogWarning($"[TUTORIAL SCANNER] Không tìm thấy UpgradeButton trên công trình: {(building != null ? building.buildingName : "null")}");
        return null;
    }

    /// <summary>
    /// 🔥 5. QUÉT NÚT SỬA CHỮA (REPAIR BUTTON)
    /// Quét các UI con của công trình thuộc Layer UI để tìm Nút Sửa Chữa
    /// </summary>
    public RectTransform GetRepairButtonTransform(UpgradeableBuilding building)
    {
        if (building == null) return null;

        // Quét tất cả Button trong Canvas con của Công trình
        Button[] buttons = building.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            // Kiểm tra theo Layer UI hoặc tên GameObject
            bool isUILayer = (uiLayer.value & (1 << btn.gameObject.layer)) != 0;
            string btnName = btn.name.ToLower();

            if (isUILayer || btnName.Contains("repair") || btnName.Contains("suachua") || btnName.Contains("fix"))
            {
                if (btnName.Contains("repair") || btnName.Contains("suachua") || btnName.Contains("fix"))
                {
                    return btn.GetComponent<RectTransform>();
                }
            }
        }

        // Fallback: Tìm qua BuildingUpgradeUI nếu có khai báo field repairButton
        BuildingUpgradeUI ui = GetBuildingUI(building);
        if (ui != null)
        {
            Transform repairTrans = ui.transform.Find("RepairButton") ?? ui.transform.Find("BtnRepair") ?? ui.transform.Find("BtnSuaChua");
            if (repairTrans != null) return repairTrans as RectTransform;
        }

        Debug.LogWarning($"[TUTORIAL SCANNER] Không tìm thấy Nút Sửa Chữa trên công trình: {building.buildingName}");
        return null;
    }

    /// <summary>
    /// 🔥 6. QUÉT NÚT (+) MỞ RỘNG ĐẤT (EXPAND LAND BUTTON)
    /// Lấy vị trí Nút (+) từ LandGridManager hoặc quét UI/Object có tên 'Expand' / 'btnNorth'
    /// </summary>
    public Transform GetExpandLandButtonTransform(ExpandDirection direction = ExpandDirection.North)
    {
        // 1. Lấy trực tiếp từ LandGridManager nếu có
        if (LandGridManager.Ins != null)
        {
            Transform targetBtn = null;
            switch (direction)
            {
                case ExpandDirection.North: targetBtn = LandGridManager.Ins.transform.Find("btnNorth") ?? LandGridManager.Ins.transform.Find("BtnNorth"); break;
                case ExpandDirection.South: targetBtn = LandGridManager.Ins.transform.Find("btnSouth") ?? LandGridManager.Ins.transform.Find("BtnSouth"); break;
                case ExpandDirection.East:  targetBtn = LandGridManager.Ins.transform.Find("btnEast")  ?? LandGridManager.Ins.transform.Find("BtnEast"); break;
                case ExpandDirection.West:  targetBtn = LandGridManager.Ins.transform.Find("btnWest")  ?? LandGridManager.Ins.transform.Find("BtnWest"); break;
            }

            if (targetBtn != null) return targetBtn;
        }

        // 2. Fallback: Quét tìm GameObject chứa nút Expand trên Scene thuộc Layer UI
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var obj in allObjects)
        {
            bool isUILayer = (uiLayer.value & (1 << obj.layer)) != 0;
            string objName = obj.name.ToLower();

            if (isUILayer && (objName.Contains("expand") || objName.Contains("btnnorth") || objName.Contains("btn_expand")))
            {
                return obj.transform;
            }
        }

        Debug.LogWarning($"[TUTORIAL SCANNER] Không tìm thấy Nút (+) Mở Rộng Đất hướng: {direction}");
        return null;
    }

    /// <summary>
    /// 7. Tìm vị trí Trại/Căn cứ lính địch
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
        EditorGUILayout.LabelField("🏷️ CHỌN TAG & LAYER QUÉT SCENE", EditorStyles.boldLabel);
        
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