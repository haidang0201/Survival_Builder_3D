using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/*
 * UIManager.cs
 * Folder: Scripts/UI/
 * Dự án: KHẨN HOANG (PENTA DEV)
 * Người thực hiện: VŨ + ĐĂNG
 * HỢP NHẤT: Quản lý HUD, Menu xây dựng, Panel nâng cấp, và tích hợp chức năng Di Chuyển Nhà.
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
    [SerializeField] private Button moveButton; // <--- BỔ SUNG NÚT DI CHUYỂN TRÊN INSPECTOR

    [Header("Upgrade Costs Text Elements")]
    [SerializeField] private TMP_Text woodCostText;
    [SerializeField] private TMP_Text stoneCostText;
    [SerializeField] private TMP_Text foodCostText;

    [Header("Upgrade Timer UI Elements")]
    [SerializeField] private Slider upgradeProgressBar;       
    [SerializeField] private TMP_Text upgradeTimerText;       

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
        
        // Đăng ký sự kiện cho nút Di Chuyển
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
    public void OnClickStoneStorageButton() => BuildingSystem.Ins.StartPlacing(BuildingType.StoneMine);
    public void OnClickFoodStorageButton() => BuildingSystem.Ins.StartPlacing(BuildingType.FoodStorage);

    // ================= UPGRADE & MOVE PANEL LOGIC =================

    public void ShowUpgradePanel(UpgradeableBuilding building)
    {
        if (building == null) return;
        selectedBuilding = building;
        if (upgradePanel != null) upgradePanel.SetActive(true);
        
        if (!building.IsUpgrading) HideUpgradeProgress();
        RefreshUpgradePanel(building);
    }

    public void RefreshUpgradePanel(UpgradeableBuilding building)
    {
        if (building == null) return;

        int displayLevel = building.CurrentLevel + 1;
        bool isMaxLevel = building.CurrentLevel >= building.MaxLevel - 1;
        bool isCurrentlyUpgrading = building.IsUpgrading;

        if (buildingNameText != null) buildingNameText.text = building.buildingName;
        if (levelText != null) levelText.text = $"Cấp {displayLevel} / {building.MaxLevel}";
        
        if (upgradeButton != null) upgradeButton.interactable = !isMaxLevel && !isCurrentlyUpgrading;
        // Đang nâng cấp thì không cho phép vừa di chuyển vừa nâng cấp để tránh lỗi Desync tọa độ
        if (moveButton != null) moveButton.interactable = !isCurrentlyUpgrading;
        
        if (upgradeButtonText != null)
        {
            if (isMaxLevel) upgradeButtonText.text = "Đã tối đa";
            else if (isCurrentlyUpgrading) upgradeButtonText.text = "Đang nâng cấp...";
            else upgradeButtonText.text = "Nâng cấp";
        }

        if (isMaxLevel)
        {
            if (woodCostText != null) woodCostText.text = "-";
            if (stoneCostText != null) stoneCostText.text = "-";
            if (foodCostText != null) foodCostText.text = "-";
        }
        else
        {
            UpgradeableBuilding.UpgradeCost cost = building.GetNextUpgradeCost();
            if (woodCostText != null) woodCostText.text = cost.woodCost.ToString();
            if (stoneCostText != null) stoneCostText.text = cost.stoneCost.ToString();
            if (foodCostText != null) foodCostText.text = cost.foodCost.ToString();
        }
    }

    public void UpdateUpgradeProgress(float currentTimer, float totalDuration)
    {
        if (upgradeProgressBar != null && !upgradeProgressBar.gameObject.activeSelf) upgradeProgressBar.gameObject.SetActive(true);
        if (upgradeTimerText != null && !upgradeTimerText.gameObject.activeSelf) upgradeTimerText.gameObject.SetActive(true);

        if (upgradeProgressBar != null)
        {
            upgradeProgressBar.maxValue = totalDuration;
            upgradeProgressBar.value = currentTimer;
        }
        if (upgradeTimerText != null)
        {
            float timeLeft = Mathf.Max(0f, totalDuration - currentTimer);
            upgradeTimerText.text = $"{timeLeft:F1}s";
        }
    }

    public void HideUpgradeProgress()
    {
        if (upgradeProgressBar != null) upgradeProgressBar.gameObject.SetActive(false);
        if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
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

    /// <summary>
    /// Xử lý khi nhấn nút Di Chuyển
    /// </summary>
    public void OnClickMoveButton()
    {
        if (selectedBuilding == null) return;

        // Gọi BuildingSystem chuyển sang trạng thái di chuyển nhà này
        if (BuildingSystem.Ins != null)
        {
            BuildingSystem.Ins.StartMoving(selectedBuilding);
        }

        // Ẩn panel thông tin đi để màn hình thoáng khi di chuyển
        HideUpgradePanel();
    }
}