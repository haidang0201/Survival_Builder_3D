using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
 * BuildingShopUI.cs
 * Folder: Scripts/UI/
 * Dự án: KHẨN HOANG (PENTA DEV)
 * Phong cách: Demacia Rising Two-Column Building Pop-up Shop
 */

public class BuildingShopUI : MonoBehaviour
{
    public static BuildingShopUI Ins { get; private set; }

    [Header("=== CẤU HÌNH HEADER & ĐÓNG cửa SỔ ===")]
    [SerializeField] private Button closeBtn;

    [Header("=== CỘT BÊN TRÁI (DANH SÁCH CÔNG TRÌNH) ===")]
    [SerializeField] private Transform itemListContainer;
    [SerializeField] private BuildingShopItemUI currentSelectedItem;

    [Header("=== CỘT BÊN PHẢI (XEM TRƯỚC CHI TIẾT & NÚT XÂY) ===")]
    [SerializeField] private Image previewArtImage;
    [SerializeField] private TextMeshProUGUI selectedNameTMP;
    [SerializeField] private TextMeshProUGUI benefitTextTMP;
    [SerializeField] private TextMeshProUGUI descriptionTMP;

    [Header("=== CHI PHÍ TÀI NGUYÊN (TMP) ===")]
    [SerializeField] private TextMeshProUGUI woodCostTMP;
    [SerializeField] private TextMeshProUGUI stoneCostTMP;
    [SerializeField] private TextMeshProUGUI foodCostTMP;

    [Header("=== NÚT XÂY DỰNG & THỜI GIAN ===")]
    [SerializeField] private Button constructBtn;
    [SerializeField] private TextMeshProUGUI buildDurationTMP;

    [Header("=== TÙY CHỈNH MÀU GIÁ ===")]
    [SerializeField] private Color affordableColor = new Color(0.2f, 0.9f, 0.3f, 1f);   // Xanh lá
    [SerializeField] private Color unaffordableColor = new Color(0.9f, 0.2f, 0.2f, 1f); // Đỏ

    private List<BuildingShopItemUI> shopItemsList = new List<BuildingShopItemUI>();

    private void Awake()
    {
        if (Ins == null) Ins = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (closeBtn != null)
        {
            closeBtn.onClick.AddListener(CloseShop);
        }

        if (constructBtn != null)
        {
            constructBtn.onClick.AddListener(OnClickConstructButton);
        }

        RefreshAllItems();
    }

    private void OnEnable()
    {
        if (BuildingUpgradeSidePanelUI.Ins != null) BuildingUpgradeSidePanelUI.Ins.ClosePanel();
        RefreshAllItems();
    }

    /// <summary>
    /// Thu thập toàn bộ các item bên cột trái và làm mới giao diện
    /// </summary>
    public void RefreshAllItems()
    {
        shopItemsList.Clear();
        if (itemListContainer != null)
        {
            shopItemsList.AddRange(itemListContainer.GetComponentsInChildren<BuildingShopItemUI>(true));
        }
        else
        {
            shopItemsList.AddRange(GetComponentsInChildren<BuildingShopItemUI>(true));
        }

        if (shopItemsList.Count > 0)
        {
            // Mặc định chọn mục đầu tiên trong danh sách
            if (currentSelectedItem == null || !shopItemsList.Contains(currentSelectedItem))
            {
                SelectBuildingItem(shopItemsList[0]);
            }
            else
            {
                SelectBuildingItem(currentSelectedItem);
            }
        }
    }

    /// <summary>
    /// Chọn 1 công trình từ danh sách cột trái và hiển thị chi tiết sang cột phải
    /// </summary>
    public void SelectBuildingItem(BuildingShopItemUI item)
    {
        if (item == null) return;

        currentSelectedItem = item;

        // 1. Đổi highlight nền kem ở danh sách cột trái
        foreach (var i in shopItemsList)
        {
            if (i != null)
            {
                i.SetSelected(i == currentSelectedItem);
            }
        }

        // 2. Cập nhật thông tin cột bên phải
        if (selectedNameTMP != null) selectedNameTMP.text = item.buildingName;
        if (benefitTextTMP != null) benefitTextTMP.text = item.benefitText;
        if (descriptionTMP != null) descriptionTMP.text = item.buildingDescription;

        if (previewArtImage != null)
        {
            if (item.artworkSprite != null)
            {
                previewArtImage.sprite = item.artworkSprite;
                previewArtImage.gameObject.SetActive(true);
            }
        }

        if (buildDurationTMP != null)
        {
            buildDurationTMP.text = item.buildDuration.ToString();
        }

        // 3. Lấy chi phí và kiểm tra đủ tiền
        RefreshCostAndAffordability(item.buildingType);
    }

    /// <summary>
    /// Cập nhật chi phí tài nguyên và bật/tắt Nút XÂY DỰNG
    /// </summary>
    private void RefreshCostAndAffordability(BuildingType type)
    {
        if (type == BuildingType.None) return;

        int woodCost = 0, stoneCost = 0, foodCost = 0;
        if (ConstructionManager.Ins != null)
        {
            var costData = ConstructionManager.Ins.GetBuildingCost(type);
            woodCost = costData.woodCost;
            stoneCost = costData.stoneCost;
            foodCost = costData.foodCost;
        }

        bool hasEnoughWood = true, hasEnoughStone = true, hasEnoughFood = true;
        bool canAfford = true;

        if (JsonDataManager.Ins != null)
        {
            hasEnoughWood = JsonDataManager.Ins.wood >= woodCost;
            hasEnoughStone = JsonDataManager.Ins.stone >= stoneCost;
            hasEnoughFood = JsonDataManager.Ins.food >= foodCost;
            canAfford = JsonDataManager.Ins.HasEnoughResources(woodCost, stoneCost, foodCost);
        }

        // Đổi màu chữ giá
        if (woodCostTMP != null)
        {
            woodCostTMP.text = woodCost.ToString();
            woodCostTMP.color = hasEnoughWood ? affordableColor : unaffordableColor;
        }

        if (stoneCostTMP != null)
        {
            stoneCostTMP.text = stoneCost.ToString();
            stoneCostTMP.color = hasEnoughStone ? affordableColor : unaffordableColor;
        }

        if (foodCostTMP != null)
        {
            foodCostTMP.text = foodCost.ToString();
            foodCostTMP.color = hasEnoughFood ? affordableColor : unaffordableColor;
        }

        // Cập nhật trạng thái Nút XÂY DỰNG
        if (constructBtn != null)
        {
            constructBtn.interactable = canAfford;
        }
    }

    /// <summary>
    /// Khi bấm Nút XÂY DỰNG màu vàng nổi bật
    /// </summary>
    private void OnClickConstructButton()
    {
        if (currentSelectedItem == null || currentSelectedItem.buildingType == BuildingType.None) return;

        if (BuildingSystem.Ins != null)
        {
            BuildingSystem.Ins.StartPlacing(currentSelectedItem.buildingType);
        }

        CloseShop();
    }

    public void CloseShop()
    {
        gameObject.SetActive(false);
    }
}