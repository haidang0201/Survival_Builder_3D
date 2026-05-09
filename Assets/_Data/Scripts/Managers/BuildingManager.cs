using UnityEngine;
using System.Collections.Generic;

public class BuildingManager : Singleton<BuildingManager>
{
    public List<BuildingCtrl> buildings = new List<BuildingCtrl>();  // Danh sách các tòa nhà trong game

    // Tìm kiếm tòa nhà theo loại công trình
    public BuildingCtrl FindBuilding(WorkerType workerType)
    {
        foreach (BuildingCtrl building in buildings)
        {
            if (building.GetBuildingType() == WorkerTypeToBuildingType(workerType))  // Sử dụng WorkerType để lấy BuildingType
            {
                return building;  // Trả về tòa nhà nếu tìm thấy
            }
        }
        return null;  // Nếu không tìm thấy, trả về null
    }

    // Hàm chuyển đổi WorkerType thành BuildingType tương ứng
    private BuildingType WorkerTypeToBuildingType(WorkerType workerType)
    {
        switch (workerType)
        {
            case WorkerType.Home:
                return BuildingType.Home;
            case WorkerType.WorkStation:
                return BuildingType.WorkStation;
            // Thêm các trường hợp khác nếu cần thiết
            default:
                return BuildingType.WorkStation;  // Mặc định trả về WorkStation
        }
    }

    // Thêm tòa nhà vào danh sách
    public void AddBuilding(BuildingCtrl building)
    {
        if (building != null && !buildings.Contains(building))
        {
            buildings.Add(building);  // Thêm tòa nhà vào danh sách nếu chưa tồn tại
        }
    }

    // Lấy về tòa nhà nhà ở (Home)
    public BuildingCtrl GetHomeBuilding()
    {
        return GetBuildingByType(WorkerType.Home);  // Tìm tòa nhà Home
    }

    // Lấy về tòa nhà công việc (WorkStation)
    public BuildingCtrl GetWorkBuilding()
    {
        return GetBuildingByType(WorkerType.WorkStation);  // Tìm tòa nhà WorkStation
    }

    // Hàm lấy tòa nhà theo loại
    private BuildingCtrl GetBuildingByType(WorkerType workerType)
    {
        // Lấy BuildingType từ WorkerType và tìm kiếm tòa nhà
        BuildingType buildingType = WorkerTypeToBuildingType(workerType);
        foreach (BuildingCtrl building in buildings)
        {
            if (building.GetBuildingType() == buildingType)
            {
                return building;  // Trả về tòa nhà theo loại
            }
        }
        return null;  // Nếu không tìm thấy, trả về null
    }
}