using UnityEngine;
using System.Collections.Generic;

public static class BattleData
{
    [System.Serializable]
    public class BuildingInfo
    {
        public BuildingType buildingType;
        public int level = 1;
        public int soldierCount = 0;
        public Vector3 originalPosition;
    }

    public static bool HasData = false;
    public static int EnemyWaveCount = 1;
    public static List<BuildingInfo> PlayerBuildings = new List<BuildingInfo>();
    public static int TotalSoldiersInBase = 0;

    /// <summary>
    /// Ghi nhận trạng thái hiện tại của Scene chính trước khi chuyển sang Battle Scene.
    /// </summary>
    /// <param name="waveEnemyCount">Số lượng Enemy thuộc Wave chuẩn bị giao tranh</param>
    public static void RecordCurrentSceneState(int waveEnemyCount)
    {
        EnemyWaveCount = Mathf.Max(1, waveEnemyCount);
        PlayerBuildings.Clear();
        TotalSoldiersInBase = 0;

        // 1. Đếm chính xác số lính thực tế đang có mặt trên map (UnitController)
        UnitController[] activeUnits = Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None);
        int realActiveSoldierCount = 0;
        foreach (var u in activeUnits)
        {
            if (u != null && u.gameObject.activeInHierarchy)
            {
                realActiveSoldierCount++;
            }
        }

        // 2. Tìm tất cả các công trình UpgradeableBuilding trong scene
        UpgradeableBuilding[] buildings = Object.FindObjectsByType<UpgradeableBuilding>(FindObjectsSortMode.None);
        
        foreach (var building in buildings)
        {
            if (building == null || !building.gameObject.activeInHierarchy) continue;

            BuildingInfo info = new BuildingInfo
            {
                buildingType = building.buildingType,
                level = building.CurrentLevel + 1,
                originalPosition = building.transform.position,
                soldierCount = 0
            };

            // Nếu là Doanh Trại, lấy số lính ĐANG HOẠT ĐỘNG THỰC TẾ của công trình đó
            SpawnSoldier spawner = building.GetComponent<SpawnSoldier>();
            if (spawner == null) spawner = building.GetComponentInChildren<SpawnSoldier>();

            if (spawner != null)
            {
                info.soldierCount = spawner.GetActiveSoldiersCount();
            }

            PlayerBuildings.Add(info);
        }

        // Đảm bảo TotalSoldiersInBase phản ánh đúng số lính thực tế
        TotalSoldiersInBase = realActiveSoldierCount;

        HasData = true;
        Debug.Log($"[BattleData] Đã lưu dữ liệu Trận Đấu: Enemy Wave = {EnemyWaveCount}, Tổng số công trình = {PlayerBuildings.Count}, Tổng lính thực tế = {TotalSoldiersInBase}");
    }

    /// <summary>
    /// Đặt lại dữ liệu trận đấu
    /// </summary>
    public static void ResetData()
    {
        HasData = false;
        EnemyWaveCount = 1;
        PlayerBuildings.Clear();
        TotalSoldiersInBase = 0;
    }
}
