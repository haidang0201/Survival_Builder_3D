using UnityEngine;

/*
 * ResourceTestTool.cs
 * Folder: Scripts/Testing/
 * Dự án: KHẨN HOANG (PENTA DEV)
 * Công dụng: Tool Cheat/Test tài nguyên, Save/Load & XÓA CÔNG TRÌNH dành cho Tester / Developer
 */

public class ResourceTestTool : MonoBehaviour
{
    [Header("Cấu Hình Cheat")]
    public int baseAmount = 100;       // Số lượng cộng/trừ cơ bản
    public bool showDebugGUI = true;   // Bật/tắt bảng hướng dẫn phím tắt trên màn hình Game

    private void Update()
    {
        if (JsonDataManager.Ins == null) return;

        // Giữ phím Shift để tăng/trừ gấp 10 lần (Cơ bản: 100 | Shift: 1000)
        bool isShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        int currentAmount = baseAmount * (isShift ? 10 : 1);

        // ──────────────────────────────────────────────
        // 1. CỘNG TÀI NGUYÊN (Phím 1 -> 4)
        // ──────────────────────────────────────────────
        if (Input.GetKeyDown(KeyCode.Alpha1)) JsonDataManager.Ins.AddGold(currentAmount);
        if (Input.GetKeyDown(KeyCode.Alpha2)) JsonDataManager.Ins.AddWood(currentAmount);
        if (Input.GetKeyDown(KeyCode.Alpha3)) JsonDataManager.Ins.AddStone(currentAmount);
        if (Input.GetKeyDown(KeyCode.Alpha4)) JsonDataManager.Ins.AddFood(currentAmount);

        // ──────────────────────────────────────────────
        // 2. TRỪ TÀI NGUYÊN (Phím 5 -> 8)
        // ──────────────────────────────────────────────
        if (Input.GetKeyDown(KeyCode.Alpha5)) JsonDataManager.Ins.AddGold(-currentAmount);
        if (Input.GetKeyDown(KeyCode.Alpha6)) JsonDataManager.Ins.AddWood(-currentAmount);
        if (Input.GetKeyDown(KeyCode.Alpha7)) JsonDataManager.Ins.AddStone(-currentAmount);
        if (Input.GetKeyDown(KeyCode.Alpha8)) JsonDataManager.Ins.AddFood(-currentAmount);

        // ──────────────────────────────────────────────
        // 3. TIỆN ÍCH TÀI NGUYÊN
        // ──────────────────────────────────────────────
        
        // Phím 9: Tăng FULL 9,999 cho tất cả tài nguyên
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            JsonDataManager.Ins.AddGold(9999);
            JsonDataManager.Ins.AddWood(9999);
            JsonDataManager.Ins.AddStone(9999);
            JsonDataManager.Ins.AddFood(9999);
            Debug.Log("[TestTool] 🚀 Đã Cheat +9999 All Tài Nguyên!");
        }

        // Phím 0: RESET TOÀN BỘ TÀI NGUYÊN VỀ 0
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            ResetAllResources();
        }

        // ──────────────────────────────────────────────
        // 4. XÓA CÔNG TRÌNH TRÊN MAP (TÍNH NĂNG MỚI)
        // ──────────────────────────────────────────────
        
        // SHIFT + DELETE: Xóa sạch TOÀN BỘ công trình trên map
        if (isShift && Input.GetKeyDown(KeyCode.Delete))
        {
            ClearAllBuildingsOnMap();
        }
        // PHÍM X hoặc DELETE: Xóa 1 công trình đang chỉ chuột vào
        else if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Delete))
        {
            DeleteBuildingUnderMouse();
        }

        // ──────────────────────────────────────────────
        // 5. TEST LỆNH SAVE / LOAD NHANH (F5 / F9)
        // ──────────────────────────────────────────────
        if (Input.GetKeyDown(KeyCode.F5))
        {
            if (BuildingSystem.Ins != null)
            {
                BuildingSystem.Ins.SaveBuildingsToSlot(1);
                Debug.Log("[TestTool] 💾 Quick Save vào Slot 1 thành công!");
            }
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            if (BuildingSystem.Ins != null)
            {
                BuildingSystem.Ins.LoadBuildingsFromSlot(1);
                Debug.Log("[TestTool] 📂 Quick Load từ Slot 1 thành công!");
            }
        }
    }

    /// <summary>
    /// Bắn Raycast từ con trỏ chuột để tìm và xóa công trình bên dưới
    /// </summary>
    private void DeleteBuildingUnderMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Tìm component UpgradeableBuilding trên vật thể va chạm (hoặc cha của nó)
            var building = hit.collider.GetComponentInParent<UpgradeableBuilding>();
            if (building != null)
            {
                string bName = building.buildingName;
                Destroy(building.gameObject);
                
                // Cập nhật lại UI số lượng/giá xây nhà nếu có
                if (ConstructionManager.Ins != null)
                {
                    ConstructionManager.Ins.UpdateAllCostUI();
                }

                Debug.Log($"[TestTool] 🗑️ Đã xóa công trình [{bName}] tại vị trí chuột!");
            }
            else
            {
                Debug.LogWarning("[TestTool] Con trỏ chuột không chỉ vào công trình nào!");
            }
        }
    }

    /// <summary>
    /// Dọn sạch toàn bộ công trình đang xuất hiện trên Map
    /// </summary>
    private void ClearAllBuildingsOnMap()
    {
        var allBuildings = FindObjectsOfType<UpgradeableBuilding>();
        int count = allBuildings.Length;

        foreach (var b in allBuildings)
        {
            Destroy(b.gameObject);
        }

        if (ConstructionManager.Ins != null)
        {
            ConstructionManager.Ins.ResetBuildingCounts();
            ConstructionManager.Ins.UpdateAllCostUI();
        }

        Debug.Log($"[TestTool] 🧹 Đã dọn sạch TOÀN BỘ {count} công trình trên Map!");
    }

    private void ResetAllResources()
    {
        if (JsonDataManager.Ins == null) return;

        JsonDataManager.Ins.AddGold(-JsonDataManager.Ins.gold);
        JsonDataManager.Ins.AddWood(-JsonDataManager.Ins.wood);
        JsonDataManager.Ins.AddStone(-JsonDataManager.Ins.stone);
        JsonDataManager.Ins.AddFood(-JsonDataManager.Ins.food);

        Debug.Log("[TestTool] 🧹 Đã Reset toàn bộ tài nguyên về 0!");
    }

    // ──────────────────────────────────────────────
    // BẢNG HƯỚNG DẪN HIỂN THỊ TRÊN MÀN HÌNH GAME
    // ──────────────────────────────────────────────
    private void OnGUI()
    {
        if (!showDebugGUI) return;

        GUI.color = Color.yellow;
        GUILayout.BeginArea(new Rect(10, 10, 340, 260), GUI.skin.box);
        
        GUILayout.Label("<b>🛠️ CHEAT / TEST TOOL (KHẨN HOANG)</b>");
        GUILayout.Label("• <b>1/2/3/4</b>: +Vàng / +Gỗ / +Đá / +Thực Phẩm");
        GUILayout.Label("• <b>5/6/7/8</b>: -Vàng / -Gỗ / -Đá / -Thực Phẩm");
        GUILayout.Label("• Giữ <b>Shift</b>: Nhân x10 lượng cộng/trừ");
        GUILayout.Label("• <b>Phím 9</b>: Cheat +9,999 All Resources");
        GUILayout.Label("• <b>Phím 0</b>: Reset sạch tài nguyên về 0");
        GUILayout.Label("• <b>Phím X / Delete</b>: Xóa nhà đang chỉ chuột");
        GUILayout.Label("• <b>Shift + Delete</b>: Xóa TOÀN BỘ nhà trên Map");
        GUILayout.Label("• <b>F5</b>: Quick Save Slot 1 | <b>F9</b>: Quick Load Slot 1");

        GUILayout.EndArea();
    }
}