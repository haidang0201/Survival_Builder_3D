using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
 * SettlementSidePanelUI.cs
 * Folder: Scripts/UI/
 * Dự án: KHẨN HOANG (PENTA DEV)
 * Phong cách: Demacia Rising Capital/Settlement Left Side Panel (Multi-Settlement Enabled)
 */

public class SettlementSidePanelUI : MonoBehaviour
{
    public static SettlementSidePanelUI Ins { get; private set; }

    [Header("=== CẤU HÌNH TIÊU ĐỀ THỦ ĐÔ ===")]
    [SerializeField] private string defaultSettlementName = "ZEFFIRA";
    [SerializeField] private int defaultSettlementLevel = 1;
    [SerializeField] private TextMeshProUGUI settlementNameTMP;
    [SerializeField] private TextMeshProUGUI settlementLevelTMP;
    [SerializeField] private Button upgradeSettlementBtn;
    [SerializeField] private TextMeshProUGUI upgradeBtnTextTMP;

    [Header("=== CONTAINER CHỨA LƯỚI CÁC Ô CÔNG TRÌNH ===")]
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private GameObject slotItemPrefab;
    [SerializeField] private int totalSlotsCount = 12; // Tổng số ô hiển thị trong Panel

    private List<SettlementSlotItemUI> activeSlotUIItems = new List<SettlementSlotItemUI>();

    private void Awake()
    {
        if (Ins == null) Ins = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (upgradeSettlementBtn != null)
        {
            upgradeSettlementBtn.onClick.AddListener(OnClickUpgradeSettlement);
        }

        UpdateHeaderVisual();
        RefreshPanel();
    }

    private void OnEnable()
    {
        RefreshPanel();
    }

    /// <summary>
    /// Cập nhật thông tin Header Thủ Đô (Tên & Cấp độ / Trạng thái Nhà Chính)
    /// </summary>
    public void UpdateHeaderVisual()
    {
        SettlementZone currentZone = (SettlementManager.Ins != null) ? SettlementManager.Ins.CurrentSettlement : null;

        string currentName = (currentZone != null) ? currentZone.settlementName : defaultSettlementName;
        int currentLevel = (currentZone != null) ? currentZone.settlementLevel : defaultSettlementLevel;
        bool isTownHallBuilt = (currentZone == null) || currentZone.isTownHallEstablished;

        if (settlementNameTMP != null) settlementNameTMP.text = currentName;

        if (settlementLevelTMP != null)
        {
            settlementLevelTMP.text = isTownHallBuilt ? $"Lv. {currentLevel}" : "<color=orange>CHƯA CÓ NHÀ CHÍNH</color>";
        }

        if (upgradeBtnTextTMP != null)
        {
            upgradeBtnTextTMP.text = isTownHallBuilt ? "Nâng cấp" : "XÂY NHÀ CHÍNH";
        }
    }

    /// <summary>
    /// Làm mới toàn bộ lưới các ô Slot công trình theo thời gian thực
    /// </summary>
    public void RefreshPanel()
    {
        if (slotsContainer == null) return;

        UpdateHeaderVisual();

        SettlementZone currentZone = (SettlementManager.Ins != null) ? SettlementManager.Ins.CurrentSettlement : null;
        bool isTownHallBuilt = (currentZone == null) || currentZone.isTownHallEstablished;

        // 1. Thu thập danh sách các ô slot hiện tại dưới slotsContainer
        activeSlotUIItems.Clear();
        activeSlotUIItems.AddRange(slotsContainer.GetComponentsInChildren<SettlementSlotItemUI>(true));

        // 2. Lấy danh sách nhà hiện có trong game (theo SettlementZone hoặc toàn map)
        List<UpgradeableBuilding> builtStructures = new List<UpgradeableBuilding>();
        if (currentZone != null && currentZone.builtStructures.Count > 0)
        {
            builtStructures.AddRange(currentZone.builtStructures);
        }
        else if (BuildingManager.Ins != null && BuildingManager.Ins.Buildings != null)
        {
            foreach (var b in BuildingManager.Ins.Buildings)
            {
                if (b == null) continue;
                UpgradeableBuilding ub = b.GetComponent<UpgradeableBuilding>();
                if (ub != null) builtStructures.Add(ub);
            }
        }

        // 3. Hiển thị danh sách các ô Slot trong Panel
        int occupiedCount = builtStructures.Count;
        int emptyUnlockedSlotsCount = 4; // Mặc định 4 ô trống mở khóa
        int maxUnlocked = occupiedCount + emptyUnlockedSlotsCount;

        int totalCount = totalSlotsCount;

        for (int i = 0; i < totalCount; i++)
        {
            SettlementSlotItemUI slotUI = GetOrCreateSlotUI(i);
            if (slotUI == null) continue;

            slotUI.gameObject.SetActive(true);

            // Nếu vùng đất CHƯA CÓ NHÀ CHÍNH, khóa toàn bộ các ô slot con bên dưới!
            if (!isTownHallBuilt)
            {
                slotUI.SetLockedSlot();
                continue;
            }

            if (i < occupiedCount)
            {
                // Ô ĐÃ CÓ NHÀ
                slotUI.SetOccupiedSlot(builtStructures[i]);
            }
            else if (i < maxUnlocked)
            {
                // Ô TRỐNG MỞ KHÓA
                Vector3 defaultPos = (currentZone != null) 
                    ? currentZone.GetSlotWorldPosition(i) 
                    : ((BuildingSystem.Ins != null) ? BuildingSystem.Ins.SelectedSlotPos : Vector3.zero);
                slotUI.SetEmptySlot(defaultPos);
            }
            else
            {
                // Ô BỊ KHÓA 🔒
                slotUI.SetLockedSlot();
            }
        }

        // Ẩn các slot dư thừa nếu có
        for (int i = totalCount; i < activeSlotUIItems.Count; i++)
        {
            if (activeSlotUIItems[i] != null) activeSlotUIItems[i].gameObject.SetActive(false);
        }
    }

    private SettlementSlotItemUI GetOrCreateSlotUI(int index)
    {
        if (index >= 0 && index < activeSlotUIItems.Count && activeSlotUIItems[index] != null)
        {
            return activeSlotUIItems[index];
        }

        if (slotItemPrefab != null && slotsContainer != null)
        {
            GameObject obj = Instantiate(slotItemPrefab, slotsContainer);
            SettlementSlotItemUI itemUI = obj.GetComponent<SettlementSlotItemUI>();
            if (itemUI != null)
            {
                activeSlotUIItems.Add(itemUI);
                return itemUI;
            }
        }

        return null;
    }

    private void OnClickUpgradeSettlement()
    {
        SettlementZone currentZone = (SettlementManager.Ins != null) ? SettlementManager.Ins.CurrentSettlement : null;

        if (currentZone != null && !currentZone.isTownHallEstablished)
        {
            // XÂY NGUYÊN NHÀ CHÍNH CHO VÙNG ĐẤT MỚI
            currentZone.EstablishTownHall();
            RefreshPanel();
            return;
        }

        Debug.Log("[SettlementSidePanelUI] Nhấn nút Nâng cấp Thủ Đô.");
        if (currentZone != null) currentZone.settlementLevel++;
        else defaultSettlementLevel++;

        UpdateHeaderVisual();
        RefreshPanel();
    }
}
