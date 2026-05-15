using System;
using System.Collections.Generic;

/*
 * GameSaveData.cs
 * Folder: Scripts/Systems/Json/
 * Người làm: DŨNG
 *
 * Class tổng hợp TOÀN BỘ dữ liệu cần lưu vào file JSON
 * Được JsonDataManager.SaveGame() serialize → file
 * Được JsonDataManager.LoadGame() deserialize ← file
 *
 * Quan hệ:
 *   BuildingManager.GetAllStates() → buildings
 *   ResourceManager.GetAllData()   → resources
 *   JsonDataManager.SaveGame(GameSaveData)
 *   JsonDataManager.LoadGame() → GameSaveData
 *
 * KHÔNG kế thừa MonoBehaviour – class thuần C#
 */

[Serializable]
public class GameSaveData
{
    // ── META ────────────────────────────────────
    public string sceneName;        // Tên scene đang chơi
    public long savedAtUnix;      // Thời điểm lưu (Unix timestamp)

    // ── CÔNG TRÌNH ──────────────────────────────
    public List<BuildingState> buildings = new List<BuildingState>();

    // ── TÀI NGUYÊN ──────────────────────────────
    public List<ResourceData> resources = new List<ResourceData>();
}