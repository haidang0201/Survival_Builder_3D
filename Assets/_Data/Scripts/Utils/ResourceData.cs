[System.Serializable]
public class ResourceData : BaseData
{
    public ResourceType resourceType;  // Loại tài nguyên (ví dụ: Wood, Gold, Stone)
    public int amount;                 // Số lượng tài nguyên

    // Thêm thuộc tính mới để mở rộng thông tin tài nguyên
    public string resourceName;         // Tên tài nguyên (ví dụ: "Wood", "Stone")
    public string resourceDescription;  // Mô tả tài nguyên
    public string resourceCategory;     // Loại nhóm tài nguyên (ví dụ: "Natural", "Craftable", v.v.)
                                        // public Sprite resourceIcon;         // Biểu tượng của tài nguyên (để hiển thị trong UI)
}