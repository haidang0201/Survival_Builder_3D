using UnityEngine;
using System.Collections.Generic;

/*
 * BuildingManager.cs
 * Folder: Scripts/Managers/
 * Người làm: DŨNG
 *
 * Quản lý toàn bộ BuildingCtrl trong scene
 * Singleton – truy cập qua BuildingManager.Ins
 * Được gắn vào Scene Master bởi ĐĂNG
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

    /// <summary>Tìm building sẵn sàng theo loại</summary>
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

    // ================= PUBLIC – SAVE =================

    /// <summary>Lấy data tất cả building để lưu JSON</summary>
    public List<BuildingData> GetAllData()
    {
        var data = new List<BuildingData>();

        foreach (var b in buildings)
            data.Add(b.GetData());

        return data;
    }
}