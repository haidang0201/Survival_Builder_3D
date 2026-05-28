using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingUpgradeUI : MonoBehaviour
{
    [Header("Khung UI Panel chính")]
    [SerializeField] private GameObject buildingDetailPanel; // Kéo 'BuildingDetail_Panel' vào

    [Header("Nút bấm chức năng")]
    [SerializeField] private Button btnUpgrade; // Kéo nút 'Up_Btn' vào

    private UpgradeableBuilding selectedBuilding; // Lưu trữ ngôi nhà đang được chọn

    private void Start()
    {
        // Gán sự kiện cho nút bấm nâng cấp trên UI
        if (btnUpgrade != null) btnUpgrade.onClick.AddListener(OnUpgradeButtonClicked);

        CloseUI(); // Vào game tự động ẩn giao diện đi cho sạch màn hình
    }

    /// <summary> Hàm mở UI khi click trúng nhà (Được gọi từ script Camera) </summary>
    public void OpenUI(UpgradeableBuilding building)
    {
        selectedBuilding = building;
        if (buildingDetailPanel != null) buildingDetailPanel.SetActive(true); // Hiện bảng UI
    }

    /// <summary> Xử lý khi người chơi click chuột vào nút Nâng Cấp </summary>
    private void OnUpgradeButtonClicked()
    {
        if (selectedBuilding != null)
        {
            selectedBuilding.NextLevel(); // Ra lệnh cho nhà dưới đất ĐỔI MODEL!

            // Tùy chọn: Nâng cấp xong muốn đóng bảng UI luôn hoặc giữ lại thì tùy bạn. 
            // Nếu muốn đóng bảng luôn thì bật dòng dưới ra:
            // CloseUI(); 
        }
    }

    public void CloseUI()
    {
        selectedBuilding = null;
        if (buildingDetailPanel != null) buildingDetailPanel.SetActive(false); // Ẩn bảng UI
    }
}