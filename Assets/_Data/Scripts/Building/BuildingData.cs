using System;

/*
 * BuildingData.cs
 * Folder: Scripts/Building/
 * Người làm: DŨNG
 *
 * Data model thuần C# – lưu config tĩnh của 1 loại công trình.
 * Dùng cho JSON save/load qua JsonDataManager.
 * KHÔNG kế thừa MonoBehaviour.
 *
 * Khác với BuildingState:
 *   BuildingData  → config mặc định / template (đọc từ ScriptableObject hoặc JSON tĩnh)
 *   BuildingState → snapshot runtime tại thời điểm save game
 */

[Serializable]
public class BuildingData : BaseData
{
    public BuildingType buildingType;           // Loại công trình (khác None)
    public string prefabName;                   // Tên prefab để Addressables load
    public SerializableVector3 defaultPosition; // Vị trí mặc định khi spawn
    public SerializableVector3 defaultRotation; // Góc xoay mặc định
    public bool isBuilt;                        // Đã xây xong chưa (dùng khi pre-place)
    public int level;                           // Cấp độ khởi tạo
}