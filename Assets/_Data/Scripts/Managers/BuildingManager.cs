using UnityEngine;
using System.Collections.Generic;

/*
 * BuildingManager.cs
 * Folder: Scripts/Managers/
 * Người làm: DŨNG
 *
 * Quản lý toàn bộ BuildingCtrl trong scene
 * Singleton – truy cập qua BuildingManager.Ins
 * Gắn vào Scene Master bởi ĐĂNG
 *
 * Luồng save:
 *   BuildingManager.GetAllStates() → List<BuildingState> → GameSaveData → JSON
 * Luồng load:
 *   JSON → GameSaveData → List<BuildingState> → BuildingManager.LoadStates()
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
        Debug.Log($"[BuildingManager] Thêm: {building.buildingType} | Tổng: {buildings.Count}");
    }

    public void RemoveBuilding(BuildingCtrl building)
    {
        if (buildings.Remove(building))
            Debug.Log($"[BuildingManager] Xóa: {building.buildingType} | Còn: {buildings.Count}");
    }

    // ================= PUBLIC – FIND =================

    /// <summary>Tìm building sẵn sàng đầu tiên theo loại</summary>
    public BuildingCtrl FindAvailable(BuildingType type)
    {
        foreach (var b in buildings)
        {
            if (b.buildingType == type && b.IsAvailable)
                return b;
        }
        return null;
    }

    /// <summary>Lấy tất cả building theo loại</summary>
    public List<BuildingCtrl> GetAllByType(BuildingType type)
    {
        var result = new List<BuildingCtrl>();

        foreach (var b in buildings)
        {
            if (b.buildingType == type)
                result.Add(b);
        }

        return result;
    }

    // ================= PUBLIC – SHORTCUT =================

    public BuildingCtrl FindHouse() => FindAvailable(BuildingType.House);
    public BuildingCtrl FindForestHut() => FindAvailable(BuildingType.ForestHut);
    public BuildingCtrl FindSawmill() => FindAvailable(BuildingType.Sawmill);
    public BuildingCtrl FindWarehouse() => FindAvailable(BuildingType.Warehouse);

    // ================= PUBLIC – SAVE / LOAD =================

    /// <summary>Gom toàn bộ BuildingState → JsonDataManager lưu JSON</summary>
    public List<BuildingState> GetAllStates()
    {
        var states = new List<BuildingState>();

        foreach (var b in buildings)
            states.Add(b.ToState());

        return states;
    }

    /// <summary>Restore toàn bộ building từ JSON sau khi load</summary>
    public void LoadStates(List<BuildingState> states)
    {
        foreach (var state in states)
        {
            var building = FindByPrefabName(state.prefabName);

            if (building != null)
                building.FromState(state);
            else
                Debug.LogWarning($"[BuildingManager] Không tìm thấy: {state.prefabName}");
        }
    }

    // ================= PRIVATE =================

    private BuildingCtrl FindByPrefabName(string prefabName)
    {
        foreach (var b in buildings)
        {
            if (b.gameObject.name == prefabName)
                return b;
        }
        return null;
    }
}