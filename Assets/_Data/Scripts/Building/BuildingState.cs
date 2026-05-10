using System;

/*
 * BuildingState.cs
 * Folder: Scripts/Building/
 * Người làm: DŨNG
 *
 * Lưu trạng thái 1 công trình để serialize ra JSON
 * Dùng trong GameSaveData.buildings (List<BuildingState>)
 *
 * Quan hệ với các class khác:
 * BuildingCtrl  → tạo ra BuildingState qua ToState()
 * BuildingManager → gom tất cả BuildingState qua GetAllStates()
 * JsonDataManager → lưu/tải List<BuildingState>
 *
 * KHÔNG kế thừa MonoBehaviour – class thuần C#
 */

[Serializable]
public class BuildingState
{
    // ── ĐỊNH DANH ───────────────────────────────
    public BuildingType buildingType;   // Loại công trình (House, Sawmill...)
    public string prefabName;     // Tên prefab để Addressables load lại

    // ── VỊ TRÍ ──────────────────────────────────
    public SerializableVector3 position;       // Vị trí trong scene
    public SerializableVector3 rotation;       // Góc xoay

    // ── TRẠNG THÁI ──────────────────────────────
    public float buildProgress;  // Tiến độ xây: 0.0 → 1.0
    public bool isBuilt;        // Đã xây xong chưa
    public bool isOccupied;     // Có worker đang làm việc không
    public int level;          // Cấp độ công trình
}