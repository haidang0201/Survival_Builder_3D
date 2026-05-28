using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct LevelData
{
    public int level;                         // Cấp độ cấu hình (Cấp 2, Cấp 3...)
    public string customName;                 // Tên hiển thị riêng biệt theo cấp (nếu cần)
    public List<ResourceData> requiredCosts;  // Sử dụng trực tiếp class ResourceData của bạn
}

[CreateAssetMenu(fileName = "NewUpgradeData", menuName = "Upgrade System/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    public string buildingID;                 // Mã định danh công trình (VD: "Main_House")
    public List<LevelData> levels;            // Danh sách cấu hình các cấp độ

    /// <summary> Tìm và trả về cấu hình chi phí của một cấp cụ thể </summary>
    public LevelData? GetLevelConfig(int level)
    {
        if (levels == null) return null;
        foreach (var cfg in levels)
        {
            if (cfg.level == level) return cfg;
        }
        return null;
    }
}