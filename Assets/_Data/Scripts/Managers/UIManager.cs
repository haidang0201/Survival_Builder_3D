using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/*
 * UIManager.cs
 * Folder: Scripts/UI/
 * Dự án: KHẨN HOANG (PENTA DEV)
 * Người thực hiện: VŨ + ĐĂNG
 * CHỨC NĂNG: Kết nối dữ liệu từ AttackTowerAI và WatchTowerAI, bóc tách cấu trúc
 * chiso_hientai và chiso_nangcap chứa các Image + Text con để đổ thông số chuẩn xác.
 */

public class UIManager : Singleton<UIManager>
{
    [Header("Old UI Panels")]
    [SerializeField] private GameObject buildMenu;
    [SerializeField] private GameObject warningUI;
    [SerializeField] private GameObject houseSelectionPanel;
    [SerializeField] private GameObject workerStatusPanel;

    [Header("Bottom UI Toolbar (Buttons)")]
    [SerializeField] private Button buildButton;
    [SerializeField] private Button toolsButton;
    [SerializeField] private Button settingButton;

    [Header("Bottom UI Toolbar (Panels)")]
    [SerializeField] private GameObject controlHintsGroup; 
    [SerializeField] private GameObject settingUI;            

    private Coroutine _fadeWarningCoroutine;

    [Header("Upgrade & Move Panel")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private TMP_Text buildingNameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text upgradeButtonText;
    [SerializeField] private Button moveButton; 

    [Header("New Features – Preview UI Elements")]
    [SerializeField] private Image currentBuildingPreviewImage;  
    [SerializeField] private Image nextBuildingPreviewImage;     
    
    [Header("Cấu trúc Cửa sổ Chỉ Số (chiso_panel)")]
    [SerializeField] private GameObject chiso_panel;          // Panel tổng quản lý hiển thị chỉ số
    
    [Space(10)]
    [Header("--- Cấu Phần Con Của chiso_hientai ---")]
    [SerializeField] private GameObject chiso_hientai_obj;    // Object cụm Hiện Tại để ẩn/hiện
    [SerializeField] private TMP_Text txt_SatThuong_HienTai;  // Text chứa chỉ số Sát thương hiện tại
    [SerializeField] private TMP_Text txt_TamBan_HienTai;     // Text chứa chỉ số Tầm bắn hiện tại
    [SerializeField] private TMP_Text txt_TocDo_HienTai;      // Text chứa chỉ số Tốc độ hiện tại

    [Space(10)]
    [Header("--- Cấu Phần Con Của chiso_nangcap ---")]
    [SerializeField] private GameObject chiso_nangcap_obj;   // Object cụm Nâng Cấp để ẩn/hiện (hoặc ẩn khi MAX)
    [SerializeField] private TMP_Text txt_SatThuong_NangCap;  // Text chứa chỉ số Sát thương cấp tiếp theo
    [SerializeField] private TMP_Text txt_TamBan_NangCap;     // Text chứa chỉ số Tầm bắn cấp tiếp theo
    [SerializeField] private TMP_Text txt_TocDo_NangCap;      // Text chứa chỉ số Tốc độ cấp tiếp theo
    [SerializeField] private TMP_Text txt_MaxLevelNotice;     // Text phụ xuất hiện chữ "MAX" hoặc "ĐÃ TỐI ĐA" khi hết cấp

    [Header("Upgrade Costs Text Elements")]
    [SerializeField] private TMP_Text woodCostText;
    [SerializeField] private TMP_Text stoneCostText;
    [SerializeField] private TMP_Text foodCostText;

    private UpgradeableBuilding selectedBuilding;

    void Start()
    {
        if (houseSelectionPanel != null) houseSelectionPanel.SetActive(true);
        if (workerStatusPanel != null) workerStatusPanel.SetActive(true);
        if (controlHintsGroup != null) controlHintsGroup.SetActive(false);
        if (settingUI != null) settingUI.SetActive(false);
        if (upgradePanel != null) upgradePanel.SetActive(false); 

        if (buildButton != null) buildButton.onClick.AddListener(ToggleBuildMenu);
        if (toolsButton != null) toolsButton.onClick.AddListener(OnClickToolsButton);
        if (settingButton != null) settingButton.onClick.AddListener(OnClickSettingButton);

        if (upgradeButton != null) upgradeButton.onClick.AddListener(OnClickUpgradeButton);
        if (moveButton != null) moveButton.onClick.AddListener(OnClickMoveButton);
    }

    // ================= BOTTOM TOOLBAR LOGIC =================

    public void ToggleBuildMenu()
    {
        if (buildMenu != null)
            buildMenu.SetActive(!buildMenu.activeSelf);
    }

    public void OnClickToolsButton()
    {
        ExitActionModes();
        if (controlHintsGroup != null) controlHintsGroup.SetActive(true);
    }

    public void OnClickSettingButton()
    {
        ExitActionModes();
        if (settingUI != null) settingUI.SetActive(!settingUI.activeSelf);
    }

    public void ExitActionModes()
    {
        if (controlHintsGroup != null) controlHintsGroup.SetActive(false);
    }

    public void EnterPlacementMode()
    {
        if (controlHintsGroup != null) controlHintsGroup.SetActive(true);
        if (buildMenu != null) buildMenu.SetActive(false);
    }

    public void ExitPlacementMode(bool shouldReopenMenu)
    {
        if (controlHintsGroup != null) controlHintsGroup.SetActive(false); 
        if (buildMenu != null) buildMenu.SetActive(shouldReopenMenu); 
    }

    // ================= WARNING UI LOGIC =================

    public void ShowWarning(string message)
    {
        if (warningUI != null) warningUI.SetActive(true);
    }

    public void HideWarning()
    {
        if (warningUI != null) warningUI.SetActive(false);
    }

    // ================= ON CLICK BUTTONS =================

    public void OnClickHouseButton() => BuildingSystem.Ins.StartPlacing(BuildingType.House);
    public void OnClickWoodCutterButton() => BuildingSystem.Ins.StartPlacing(BuildingType.WoodCutter);
    public void OnClickStoneMineButton() => BuildingSystem.Ins.StartPlacing(BuildingType.StoneMine);
    public void OnClickKitchenButton() => BuildingSystem.Ins.StartPlacing(BuildingType.Kitchen);
    public void OnClickFoodStorageButton() => BuildingSystem.Ins.StartPlacing(BuildingType.FoodStorage);
    public void OnClickStoneStorageButton() => BuildingSystem.Ins.StartPlacing(BuildingType.StoneStorage);
    public void OnClickWarehouseButton() => BuildingSystem.Ins.StartPlacing(BuildingType.Warehouse);

    public void OnClickWatchTowerButton() => BuildingSystem.Ins.StartPlacing(BuildingType.WatchTower);
    public void OnClickArcherTowerButton() => BuildingSystem.Ins.StartPlacing(BuildingType.ArcherTower);
    public void OnClickCannonButton() => BuildingSystem.Ins.StartPlacing(BuildingType.Cannon);

    public void OnClickBarracksMeleeButton() => BuildingSystem.Ins.StartPlacing(BuildingType.BarracksMelee);
    public void OnClickBarracksArcherButton() => BuildingSystem.Ins.StartPlacing(BuildingType.BarracksArcher);
    public void OnClickBarracksSpearButton() => BuildingSystem.Ins.StartPlacing(BuildingType.BarracksSpear);

    // ================= UPGRADE & MOVE PANEL LOGIC =================

    public void ShowUpgradePanel(UpgradeableBuilding building)
    {
        if (building == null) return;
        selectedBuilding = building;
        if (upgradePanel != null) upgradePanel.SetActive(true);
        
        RefreshUpgradePanel(building);
    }

    public void RefreshUpgradePanel(UpgradeableBuilding building)
    {
        if (building == null) return;

        int currentLevelIdx = building.CurrentLevel; 
        int displayLevel = currentLevelIdx + 1;       
        bool isMaxLevel = building.CurrentLevel >= building.MaxLevel - 1;
        bool isCurrentlyUpgrading = building.IsUpgrading;

        if (buildingNameText != null) buildingNameText.text = building.buildingName;
        if (levelText != null) levelText.text = $"Cấp {displayLevel} / {building.MaxLevel}";
        
        if (upgradeButton != null) upgradeButton.interactable = !isMaxLevel && !isCurrentlyUpgrading;
        if (moveButton != null) moveButton.interactable = !isCurrentlyUpgrading;
        
        if (upgradeButtonText != null)
        {
            if (isMaxLevel) upgradeButtonText.text = "Đã tối đa";
            else if (isCurrentlyUpgrading) upgradeButtonText.text = "Đang nâng cấp...";
            else upgradeButtonText.text = "Nâng cấp";
        }

        // --- LUỒNG XỬ LÝ HÌNH ẢNH PREVIEW ---
        if (building.BuildingIcons != null)
        {
            if (currentBuildingPreviewImage != null && currentLevelIdx < building.BuildingIcons.Length)
            {
                currentBuildingPreviewImage.sprite = building.BuildingIcons[currentLevelIdx];
                currentBuildingPreviewImage.gameObject.SetActive(true);
            }

            if (nextBuildingPreviewImage != null)
            {
                if (!isMaxLevel && (currentLevelIdx + 1) < building.BuildingIcons.Length)
                {
                    nextBuildingPreviewImage.sprite = building.BuildingIcons[currentLevelIdx + 1];
                    nextBuildingPreviewImage.gameObject.SetActive(true);
                }
                else
                {
                    nextBuildingPreviewImage.gameObject.SetActive(false); 
                }
            }
        }

        // --- LUỒNG PHÂN TÁCH CHỈ SỐ VÀO CÁC Ô TẠO SẴN ---
        bool isDefenseTower = building.buildingType == BuildingType.WatchTower || 
                              building.buildingType == BuildingType.ArcherTower || 
                              building.buildingType == BuildingType.Cannon;

        if (isDefenseTower)
        {
            if (chiso_panel != null) chiso_panel.SetActive(true);
            UpdateDetailedTowerStats(building, currentLevelIdx, isMaxLevel);
        }
        else
        {
            if (chiso_panel != null) chiso_panel.SetActive(false);
        }

        // --- LUỒNG HIỂN THỊ CHI PHÍ ---
        if (isMaxLevel)
        {
            if (woodCostText != null) woodCostText.text = "-";
            if (stoneCostText != null) stoneCostText.text = "-";
            if (foodCostText != null) foodCostText.text = "-";
        }
        else
        {
            UpgradeableBuilding.UpgradeCost cost = building.GetNextUpgradeCost();
            if (woodCostText != null) woodCostText.text = MathUtility_FormatCost(cost.woodCost);
            if (stoneCostText != null) stoneCostText.text = MathUtility_FormatCost(cost.stoneCost);
            if (foodCostText != null) foodCostText.text = MathUtility_FormatCost(cost.foodCost);
        }
    }

    private string MathUtility_FormatCost(int rawCost)
    {
        return rawCost.ToString();
    }

    /// <summary>
    /// Bóc tách thông số tháp phòng thủ và điền chuẩn xác vào từng thành phần TMP_Text con bên trong panel chỉ số
    /// </summary>
    private void UpdateDetailedTowerStats(UpgradeableBuilding building, int currentLv, bool isMax)
    {
        float curDamage = 0, nxtDamage = 0;
        float curRange = 0, nxtRange = 0; 
        float curSpeed = 0, nxtSpeed = 0;

        // 1. LẤY SCRIPT AI CỦA CẤP HIỆN TẠI TỪ MẢNG TRÊN THẰNG CHA
        AttackTowerAI currentAttackAI = null;
        if (building.TowerLevelScripts != null && currentLv >= 0 && currentLv < building.TowerLevelScripts.Length)
        {
            currentAttackAI = building.TowerLevelScripts[currentLv];
        }

        // Vẫn giữ kiểm tra tháp canh dự phòng nếu có
        WatchTowerAI watchAI = building.GetComponent<WatchTowerAI>();

        // KÍCH HOẠT HIỂN THỊ CỤM HIỆN TẠI MẶC ĐỊNH
        if (chiso_hientai_obj != null) chiso_hientai_obj.SetActive(true);

        // XỬ LÝ CHO THÁP TẤN CÔNG (ĐÃ CÓ SCRIPT CẤP HIỆN TẠI)
        if (currentAttackAI != null)
        {
            curSpeed = currentAttackAI.fireRate;
            curRange = currentAttackAI.AttackRange; // Lấy thuộc tính AttackRange từ script cấp hiện tại

            // Xác định sát thương cấp hiện tại (Dựa theo thiết kế của script cấp đó)
            if (currentLv == 0) curDamage = currentAttackAI.damageLv1;
            else if (currentLv == 1) curDamage = currentAttackAI.damageLv2;
            else curDamage = currentAttackAI.damageLv3;

            // Điền thông số vào cụm Hiện Tại trên UI
            if (txt_SatThuong_HienTai != null) txt_SatThuong_HienTai.text = curDamage.ToString();
            if (txt_TamBan_HienTai != null) txt_TamBan_HienTai.text = $"{curRange}m";
            if (txt_TocDo_HienTai != null) txt_TocDo_HienTai.text = $"{curSpeed}/s";

            // 2. LẤY SCRIPT AI CỦA CẤP TIẾP THEO ĐỂ HIỂN THỊ THÔNG SỐ NÂNG CẤP
            if (isMax)
            {
                if (chiso_nangcap_obj != null) chiso_nangcap_obj.SetActive(false);
                if (txt_MaxLevelNotice != null)
                {
                    txt_MaxLevelNotice.gameObject.SetActive(true);
                    txt_MaxLevelNotice.text = "CẤP TỐI ĐA";
                }
            }
            else
            {
                if (chiso_nangcap_obj != null) chiso_nangcap_obj.SetActive(true);
                if (txt_MaxLevelNotice != null) txt_MaxLevelNotice.gameObject.SetActive(false);

                int nextLv = currentLv + 1;
                AttackTowerAI nextAttackAI = null;

                // Bốc thử script của cấp tiếp theo trong mảng
                if (building.TowerLevelScripts != null && nextLv < building.TowerLevelScripts.Length)
                {
                    nextAttackAI = building.TowerLevelScripts[nextLv];
                }

                if (nextAttackAI != null)
                {
                    nxtSpeed = nextAttackAI.fireRate;
                    nxtRange = nextAttackAI.AttackRange;

                    if (nextLv == 1) nxtDamage = nextAttackAI.damageLv2;
                    else nxtDamage = nextAttackAI.damageLv3;
                }
                else
                {
                    // Fallback dự phòng nếu chưa kéo thả script cấp kế tiếp
                    nxtDamage = curDamage;
                    nxtRange = curRange;
                    nxtSpeed = curSpeed;
                }

                // Điền thông số mới vào cụm Nâng Cấp (Đổi màu xanh trực quan nếu thông số tăng lên)
                if (txt_SatThuong_NangCap != null) 
                    txt_SatThuong_NangCap.text = nxtDamage > curDamage ? $"<color=green>{nxtDamage}</color>" : nxtDamage.ToString();
                
                if (txt_TamBan_NangCap != null) 
                    txt_TamBan_NangCap.text = nxtRange > curRange ? $"<color=green>{nxtRange}m</color>" : $"{nxtRange}m";
                
                if (txt_TocDo_NangCap != null) 
                    txt_TocDo_NangCap.text = nxtSpeed > curSpeed ? $"<color=green>{nxtSpeed}/s</color>" : $"{nxtSpeed}/s";
            }
        }
        // XỬ LÝ CHO THÁP CANH (Dự phòng cấu trúc cũ)
        else if (watchAI != null)
        {
            curRange = watchAI.detectRadius;
            curSpeed = watchAI.scanInterval;

            if (txt_SatThuong_HienTai != null) txt_SatThuong_HienTai.text = "-";
            if (txt_TamBan_HienTai != null) txt_TamBan_HienTai.text = $"{curRange}m";
            if (txt_TocDo_HienTai != null) txt_TocDo_HienTai.text = $"{curSpeed}s";

            if (isMax)
            {
                if (chiso_nangcap_obj != null) chiso_nangcap_obj.SetActive(false);
                if (txt_MaxLevelNotice != null)
                {
                    txt_MaxLevelNotice.gameObject.SetActive(true);
                    txt_MaxLevelNotice.text = "CẤP TỐI ĐA";
                }
            }
            else
            {
                if (chiso_nangcap_obj != null) chiso_nangcap_obj.SetActive(true);
                if (txt_MaxLevelNotice != null) txt_MaxLevelNotice.gameObject.SetActive(false);

                float nxtWatchRange = curRange + 5f; 

                if (txt_SatThuong_NangCap != null) txt_SatThuong_NangCap.text = "-";
                if (txt_TamBan_NangCap != null) txt_TamBan_NangCap.text = $"<color=green>{nxtWatchRange}m</color>";
                if (txt_TocDo_NangCap != null) txt_TocDo_NangCap.text = $"{curSpeed}s";
            }
        }
    }

    public void HideUpgradePanel()
    {
        selectedBuilding = null;
        if (upgradePanel != null) upgradePanel.SetActive(false);
    }

    public void OnClickUpgradeButton()
    {
        if (selectedBuilding == null || selectedBuilding.IsUpgrading) return;

        UpgradeableBuilding.UpgradeCost cost = selectedBuilding.GetNextUpgradeCost();
        if (ResourceManager.Instance != null && !ResourceManager.Instance.Consume(cost.woodCost, cost.foodCost, cost.stoneCost)) return;

        selectedBuilding.StartUpgradeProcess();
        RefreshUpgradePanel(selectedBuilding);
    }

    public void OnClickMoveButton()
    {
        if (selectedBuilding == null) return;

        if (BuildingSystem.Ins != null)
        {
            BuildingSystem.Ins.StartMoving(selectedBuilding);
        }

        HideUpgradePanel();
    }

    public void CloseUpgradePanel()
    {
        selectedBuilding = null;
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
            Debug.Log("[UIManager] ❌ Đã đóng Cửa sổ Nâng cấp / Di chuyển công trình.");
        }
    }

    public void CloseBuildMenu()
    {
        if (buildMenu != null)
        {
            buildMenu.SetActive(false);
            Debug.Log("[UIManager] ❌ Đã đóng Menu Xây dựng.");
        }
    }

    public void CloseAllActiveWindows()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);
        if (buildMenu != null) buildMenu.SetActive(false);
        if (settingUI != null) settingUI.SetActive(false);
        if (houseSelectionPanel != null) houseSelectionPanel.SetActive(false);
        if (workerStatusPanel != null) workerStatusPanel.SetActive(false);
        
        selectedBuilding = null;
        Debug.Log("[UIManager] 🧹 Đã dọn dẹp và ẩn toàn bộ giao diện cửa sổ popup.");
    }
}