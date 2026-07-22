using UnityEngine;
using System.Collections.Generic;

/*
 * BuildingManager.cs
 * Folder: Scripts/Managers/
 * Người làm: DŨNG (Logic) + ĐĂNG (Kiến trúc & Singleton Master)
 * Dự án: KHẨN HOANG (PENTA DEV)
 *
 * NHIỆM VỤ: Quản lý tập trung toàn bộ thực thể công trình (BuildingCtrl) trong scene.
 * KIẾN TRÚC: Kế thừa Generic Singleton<T> – truy cập toàn cục qua BuildingManager.Ins
 *
 * API CHUẨN (class khác phải dùng đúng tên):
 *   AddBuilding / RemoveBuilding   – đăng ký / huỷ đăng ký
 *   FindAvailable(type)            – tìm công trình sẵn sàng cho worker
 *   CanBuild(pos, type, ignore)    – kiểm tra vị trí có bị chồng không
 *   GetAllStates()                 – gom trạng thái để save
 *   LoadStates(states)             – restore từ save
 */

public class BuildingManager : Singleton<BuildingManager>
{
    // ================= DATA =================

    private readonly List<BuildingCtrl> buildings = new List<BuildingCtrl>();

    /// <summary>
    /// ReadOnly để các hệ thống khác (AI, UI) duyệt mà không sửa trực tiếp danh sách.
    /// </summary>
    public IReadOnlyList<BuildingCtrl> Buildings => buildings;


    // ================= ĐĂNG KÝ / HUỶ ĐĂNG KÝ =================

    /// <summary>
    /// Đăng ký công trình vào hệ thống. Gọi tự động từ BuildingCtrl.Start().
    /// </summary>
    public void AddBuilding(BuildingCtrl building)
    {
        if (building == null) return;
        if (buildings.Contains(building)) return;

        if (building.buildingType == BuildingType.None)
        {
            Debug.LogError($"[BuildingManager] ❌ BuildingType chưa thiết lập (None) trên: {building.gameObject.name}");
            return;
        }

        // Kiểm tra chồng lấn với công trình đã có (bỏ qua chính nó)
        if (!CanBuild(building.transform.position, building.buildingType, building))
        {
            Debug.LogWarning($"[BuildingManager] ⚠️ Chồng lấn vị trí khi đăng ký {building.buildingType} tại {building.transform.position}");
            return;
        }

        buildings.Add(building);
        Debug.Log($"[BuildingManager] ➕ Đã đăng ký: {building.buildingType} ({building.gameObject.name})");
    }

    /// <summary>
    /// Gỡ công trình khỏi danh sách. Gọi tự động từ BuildingCtrl.OnDestroy().
    /// </summary>
    public void RemoveBuilding(BuildingCtrl building)
    {
        if (building == null) return;
        if (!buildings.Contains(building)) return;

        buildings.Remove(building);
        Debug.Log($"[BuildingManager] ➖ Đã xoá: {building.buildingType} ({building.gameObject.name})");
    }


    // ================= TÌM KIẾM =================

    /// <summary>
    /// Tìm công trình đầu tiên thuộc loại chỉ định đang sẵn sàng (đã xây xong và không có worker).
    /// Dùng cho AI Nông dân: tìm Kitchen để giao lương thực, tìm Warehouse để cất tài nguyên...
    /// </summary>
    public BuildingCtrl FindAvailable(BuildingType type)
    {
        foreach (var b in buildings)
        {
            if (b != null && b.buildingType == type && b.IsAvailable)
                return b;
        }
        return null;
    }

    /// <summary>
    /// Lấy tất cả công trình thuộc loại chỉ định (bất kể trạng thái).
    /// </summary>
    public List<BuildingCtrl> FindAll(BuildingType type)
    {
        var result = new List<BuildingCtrl>();
        foreach (var b in buildings)
        {
            if (b != null && b.buildingType == type)
                result.Add(b);
        }
        return result;
    }


    // ================= SAVE / LOAD =================

    /// <summary>
    /// Gom trạng thái tất cả công trình để lưu JSON.
    /// Gọi từ BuildingSystem.SaveBuildings() hoặc JsonDataManager.
    /// </summary>
    public List<BuildingState> GetAllStates()
    {
        var states = new List<BuildingState>();
        foreach (var b in buildings)
        {
            if (b != null)
            {
                BuildingState state = b.ToState();

                // 🔥 LẤY CẤP ĐỘ HIỆN TẠI LƯU VÀO STATE
                var upgradeable = b.GetComponent<UpgradeableBuilding>();
                if (upgradeable != null)
                {
                    state.level = upgradeable.CurrentLevel;
                }

                states.Add(state);
            }
        }
        return states;
    }

    /// <summary>
    /// Xoá toàn bộ công trình cũ và tái dựng từ danh sách save.
    /// Gọi từ BuildingSystem.LoadBuildings().
    /// </summary>
    public void LoadStates(List<BuildingState> states)
    {
        if (states == null) return;

        // Bước 1: Dọn sạch scene
        ClearAll();

        // Bước 2: Tái tạo từng công trình từ state
        foreach (var state in states)
        {
            if (state == null || state.buildingType == BuildingType.None) continue;

            BuildingCtrl spawned = ConstructionManager.Ins.SpawnBuilding(
                state.buildingType,
                state.position.ToVector3(),
                Quaternion.Euler(state.rotation.ToVector3())
            );

            if (spawned != null)
            {
                spawned.FromState(state);

                // 🔥 ÉP CÔNG TRÌNH KHÔI PHỤC ĐÚNG LEVEL VÀ TẮT ĐẾM GIỜ XÂY LẠI TỪ ĐẦU
                UpgradeableBuilding upgradeable = spawned.GetComponent<UpgradeableBuilding>();
                if (upgradeable != null)
                {
                    upgradeable.LoadLevel(state.level);
                }
            }
            else
            {
                Debug.LogError($"[BuildingManager] ❌ Khôi phục thất bại: {state.buildingType}. Kiểm tra Prefab trong ConstructionManager!");
            }
        }
    }

    /// <summary>Phá hủy toàn bộ công trình hiện có – chỉ gọi trước LoadStates().</summary>
    private void ClearAll()
    {
        for (int i = buildings.Count - 1; i >= 0; i--)
        {
            if (buildings[i] != null)
                Destroy(buildings[i].gameObject);
        }
        buildings.Clear();
        Debug.Log("[BuildingManager] 🗑️ Đã dọn sạch toàn bộ công trình trong scene.");
    }


    // ================= KIỂM TRA VỊ TRÍ =================

    /// <summary>
    /// Kiểm tra vị trí có thể xây được không (không chồng lên công trình khác).
    ///
    /// Lưu ý: kiểm tra với TẤT CẢ công trình đang tồn tại (kể cả đang xây dở),
    /// không chỉ những công trình IsAvailable – tránh chồng lấn khi đặt mới.
    ///
    /// ignoreBuilding: bỏ qua chính nó khi BuildingCtrl.Start() tự đăng ký.
    /// </summary>
    public bool CanBuild(Vector3 position, BuildingType buildingType, BuildingCtrl ignoreBuilding = null)
    {
        Bounds testBounds = new Bounds(position, Vector3.one); // Kích thước tạm – GhostBuilding dùng OverlapBox chính xác hơn

        foreach (var b in buildings)
        {
            if (b == null || b == ignoreBuilding) continue;

            Collider col = b.GetComponent<Collider>();
            if (col == null) continue;

            // Cập nhật kích thước test theo collider thực tế của công trình đã có
            Bounds testBoundsActual = new Bounds(position, col.bounds.size);
            if (col.bounds.Intersects(testBoundsActual))
                return false;
        }

        return true;
    }
}