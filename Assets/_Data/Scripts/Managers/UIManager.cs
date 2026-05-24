using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private GameObject controlHintsGroup; // Bảng hướng dẫn đặt/xoay nhà
    [SerializeField] private GameObject settingUI;         // Bảng cài đặt riêng biệt

    void Start()
    {
        // Giữ nguyên logic cũ của bạn
        if (houseSelectionPanel != null) houseSelectionPanel.SetActive(true);
        if (workerStatusPanel != null) workerStatusPanel.SetActive(true);

        // Mặc định vào game ẩn bảng hướng dẫn và bảng cài đặt đi
        if (controlHintsGroup != null) controlHintsGroup.SetActive(false);
        if (settingUI != null) settingUI.SetActive(false);

        // Đăng ký sự kiện Click tự động cho các nút bấm dưới Toolbar
        if (buildButton != null) buildButton.onClick.AddListener(ToggleBuildMenu);
        if (toolsButton != null) toolsButton.onClick.AddListener(OnClickToolsButton);
        if (settingButton != null) settingButton.onClick.AddListener(OnClickSettingButton);
    }

    void Update()
    {
        // Nếu đang trong chế độ hành động (bảng hướng dẫn đang hiện)
        // Mà người chơi bấm CHUỘT PHẢI, ta sẽ hủy chế độ đó và ẩn bảng đi
        if (controlHintsGroup != null && controlHintsGroup.activeSelf)
        {
            if (Input.GetMouseButtonDown(1)) // 1 là Chuột phải
            {
                ExitActionModes();
                // Nếu BuildingSystem của bạn có hàm hủy đặt, bạn gọi thêm ở đây:
                // BuildingSystem.Ins.CancelPlacing(); 
                Debug.Log("Đã hủy chế độ xây dựng bằng Chuột Phải.");
            }
        }
    }

    // ================= BOTTOM TOOLBAR LOGIC =================

    // Hàm Bật/Tắt Menu chọn danh mục xây dựng chính
    public void ToggleBuildMenu()
    {
        if (buildMenu != null)
        {
            buildMenu.SetActive(!buildMenu.activeSelf);
        }
    }

    // Hàm bấm vào nút Bộ công cụ
    public void OnClickToolsButton()
    {
        ExitActionModes();
        if (controlHintsGroup != null) controlHintsGroup.SetActive(true);
        Debug.Log("Đã chọn bộ công cụ.");
    }

    // Hàm bấm vào nút Cài đặt
    public void OnClickSettingButton()
    {
        ExitActionModes();
        if (settingUI != null)
        {
            settingUI.SetActive(!settingUI.activeSelf);
        }
    }

    // Hàm bổ trợ ẩn bảng hướng dẫn phím tắt
    public void ExitActionModes()
    {
        if (controlHintsGroup != null) controlHintsGroup.SetActive(false);
    }

    // Hàm dùng để kích hoạt bảng hướng dẫn (gọi nội bộ khi chọn xong công trình cụ thể)
    private void EnterPlacementMode()
    {
        if (controlHintsGroup != null) controlHintsGroup.SetActive(true);
        if (buildMenu != null) buildMenu.SetActive(false); // Tự động ẩn menu chọn nhà khi đang đi đặt nhà
    }


    // ================= OLD WARNING LOGIC =================

    public void ShowWarning(string message)
    {
        if (warningUI != null) warningUI.SetActive(true);
    }

    public void HideWarning()
    {
        if (warningUI != null) warningUI.SetActive(false);
    }


    // ================= BUILDING BUTTONS =================

    public void OnClickHouseButton()
    {
        EnterPlacementMode();
        BuildingSystem.Ins.StartPlacing(BuildingType.House);
    }

    public void OnClickForestHutButton()
    {
        EnterPlacementMode();
        BuildingSystem.Ins.StartPlacing(BuildingType.ForestHut);
    }

    public void OnClickSawmillButton()
    {
        EnterPlacementMode();
        BuildingSystem.Ins.StartPlacing(BuildingType.Sawmill);
    }

    public void OnClickWarehouseButton()
    {
        EnterPlacementMode();
        BuildingSystem.Ins.StartPlacing(BuildingType.Warehouse);
    }

    public void OnClickHouseBuilderButton()
    {
        EnterPlacementMode();
        BuildingSystem.Ins.StartPlacing(BuildingType.House);
    }
}