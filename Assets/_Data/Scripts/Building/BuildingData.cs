using System;

/*
 * BuildingData.cs
 * Folder: Scripts/Building/
 * Người làm: DŨNG
 *
 * Data model thuần C# – lưu config và trạng thái 1 công trình
 * Dùng cho JSON save/load qua JsonDataManager
 * KHÔNG kế thừa MonoBehaviour
 */

[Serializable]
public class BuildingData : BaseData
{
    public BuildingType buildingType;    // Loại công trình
    public string prefabName;      // Tên prefab để Addressables load
    public SerializableVector3 defaultPosition; // Vị trí mặc định
    public SerializableVector3 defaultRotation; // Góc xoay mặc định
    public bool isBuilt;         // Đã xây xong chưa
    public int level;           // Cấp độ (dùng sau nếu có nâng cấp)
}