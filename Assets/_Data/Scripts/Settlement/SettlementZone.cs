using System.Collections.Generic;
using UnityEngine;

/*
 * SettlementZone.cs
 * Folder: Scripts/Settlement/
 * Dự án: KHẨN HOANG (PENTA DEV)
 * Phong cách: Demacia Rising Multi-Settlement Stage Territory Data
 */

public class SettlementZone : MonoBehaviour
{
    [Header("=== THÔNG TIN VÙNG ĐẤT / ẢI ===")]
    public string settlementName = "ZEFFIRA";
    public int settlementLevel = 1;
    public bool isUnlocked = true;                     // Vùng đất đã được mở khóa trên bản đồ chưa
    public bool isTownHallEstablished = true;           // Đã xây Nhà Chính chưa (Vùng đất khởi đầu = true)

    [Header("=== VỊ TRÍ 3D CỦA NHÀ CHÍNH & CÁC Ô SLOT ===")]
    public Transform townHallPoint;                     // Vị trí đặt Nhà Chính ở trung tâm
    public GameObject townHallPrefab;                   // Prefab Nhà Chính khi khởi tạo
    public List<Transform> slotPoints = new List<Transform>(); // Danh sách các mốc vị trí 3D của ô slot

    [Header("=== CHI PHÍ XÂY NHÀ CHÍNH CHO VÙNG ĐẤT MỚI ===")]
    public int establishWoodCost = 100;
    public int establishStoneCost = 100;
    public int establishFoodCost = 50;

    [HideInInspector]
    public UpgradeableBuilding townHallBuilding;        // Building Nhà chính hiện tại
    [HideInInspector]
    public List<UpgradeableBuilding> builtStructures = new List<UpgradeableBuilding>(); // Danh sách công trình đã xây

    private void Awake()
    {
        if (townHallPoint == null) townHallPoint = transform;
    }

    /// <summary>
    /// Xây dựng Nhà Chính cho vùng đất mới khi người chơi bấm "XÂY NHÀ CHÍNH"
    /// </summary>
    public bool EstablishTownHall()
    {
        if (isTownHallEstablished) return true;

        // Trừ tài nguyên
        if (JsonDataManager.Ins != null)
        {
            if (!JsonDataManager.Ins.HasEnoughResources(establishWoodCost, establishStoneCost, establishFoodCost))
            {
                if (UIManager.Ins != null) UIManager.Ins.ShowWarning("Không đủ tài nguyên xây Nhà Chính!");
                return false;
            }

            JsonDataManager.Ins.AddWood(-establishWoodCost);
            JsonDataManager.Ins.AddStone(-establishStoneCost);
            JsonDataManager.Ins.AddFood(-establishFoodCost);
        }

        // Tạo Nhà Chính trong thế giới 3D
        if (townHallPrefab != null && townHallPoint != null)
        {
            GameObject obj = Instantiate(townHallPrefab, townHallPoint.position, townHallPoint.rotation, transform);
            townHallBuilding = obj.GetComponent<UpgradeableBuilding>();
        }

        isTownHallEstablished = true;
        Debug.Log($"[SettlementZone] 🎉 Đã xây dựng thành công Nhà Chính cho vùng đất: {settlementName}!");

        if (SettlementSidePanelUI.Ins != null) SettlementSidePanelUI.Ins.RefreshPanel();
        return true;
    }

    /// <summary>
    /// Lấy vị trí 3D cho ô Slot theo Index
    /// </summary>
    public Vector3 GetSlotWorldPosition(int index)
    {
        if (index >= 0 && index < slotPoints.Count && slotPoints[index] != null)
        {
            return slotPoints[index].position;
        }
        return transform.position;
    }
}
