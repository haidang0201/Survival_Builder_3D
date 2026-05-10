using UnityEngine;
using System.Collections.Generic;

/*
 * BuildingManager.cs
 * Folder: Scripts/Managers/
 * Người làm: DŨNG
 *
 * Quản lý toàn bộ BuildingCtrl trong scene
 * Singleton – gắn vào Scene Master bởi ĐĂNG
 *
 * Nguyên tắc Save/Load:
 *   - Chỉ lưu khi người chơi nhấn Space
 *   - Load → xóa TOÀN BỘ building hiện tại → spawn lại từ save
 *   - Nếu chưa có file save → không xóa gì cả
 */

public class BuildingManager : Singleton<BuildingManager>
{
    // ================= DATA =================

    private readonly List<BuildingCtrl> buildings = new List<BuildingCtrl>();

    // ================= PUBLIC – REGISTER =================

    public void AddBuilding(BuildingCtrl building)
    {
        if (building == null || buildings.Contains(building)) return;

        buildings.Add(building);
    }

    public void RemoveBuilding(BuildingCtrl building)
    {
        buildings.Remove(building);
    }

    // ================= PUBLIC – FIND =================

    public BuildingCtrl FindAvailable(BuildingType type)
    {
        foreach (var b in buildings)
            if (b.buildingType == type && b.IsAvailable) return b;
        return null;
    }

    public List<BuildingCtrl> GetAllByType(BuildingType type)
    {
        var result = new List<BuildingCtrl>();
        foreach (var b in buildings)
            if (b.buildingType == type) result.Add(b);
        return result;
    }

    // ================= PUBLIC – SHORTCUT =================

    public BuildingCtrl FindHouse() => FindAvailable(BuildingType.House);
    public BuildingCtrl FindForestHut() => FindAvailable(BuildingType.ForestHut);
    public BuildingCtrl FindSawmill() => FindAvailable(BuildingType.Sawmill);
    public BuildingCtrl FindWarehouse() => FindAvailable(BuildingType.Warehouse);

    // ================= PUBLIC – SAVE =================

    /// <summary>Gom trạng thái tất cả building để JsonDataManager lưu JSON</summary>
    public List<BuildingState> GetAllStates()
    {
        var states = new List<BuildingState>();
        foreach (var b in buildings)
            states.Add(b.ToState());
        return states;
    }

    // ================= PUBLIC – LOAD =================

    /// <summary>
    /// Load từ save:
    /// 1. Xóa toàn bộ building hiện tại (kể cả chưa lưu)
    /// 2. Spawn lại đúng theo dữ liệu đã lưu
    /// Chỉ gọi khi đã xác nhận có file save hợp lệ
    /// </summary>
    public void LoadStates(List<BuildingState> states)
    {
        if (ConstructionManager.Ins == null)
        {
            return;
        }

        ClearAll();

        foreach (var state in states)
        {
            BuildingCtrl spawned = ConstructionManager.Ins.SpawnBuilding(
                state.buildingType,
                state.position.ToVector3(),
                Quaternion.Euler(state.rotation.ToVector3())
            );

            if (spawned != null)
                spawned.FromState(state);
        }
    }

    private void ClearAll()
    {
        for (int i = buildings.Count - 1; i >= 0; i--)
        {
            if (buildings[i] != null)
                Destroy(buildings[i].gameObject);
        }

        buildings.Clear();
    }
}