using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    [Header("Old UI Panels")]
    [SerializeField] private GameObject buildMenu;
    [SerializeField] private GameObject warningUI;
    [SerializeField] private GameObject houseSelectionPanel;
    [SerializeField] private GameObject workerStatusPanel;

    [Header("Bottom UI (New Toolbar)")]
    [SerializeField] private GameObject controlHintsGroup; // Bảng hướng dẫn đặt/xoay nhà
    [SerializeField] private GameObject settingUI;         // Bảng cài đặt có sẵn của bạn

    void Start()
    {
        // Giữ nguyên logic cũ của bạn
        if (houseSelectionPanel != null) houseSelectionPanel.SetActive(true);
        if (workerStatusPanel != null) workerStatusPanel.SetActive(true);

        // Mặc định vào game ẩn bảng hướng dẫn và bảng cài đặt đi
        if (controlHintsGroup != null) controlHintsGroup.SetActive(false);
        if (settingUI != null) settingUI.SetActive(false);
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

    // Hàm gắn vào Build_Btn ở thanh công cụ dưới để Bật/Tắt Menu xây dựng chính
    public void ToggleBuildMenu()
    {
        if (buildMenu != null)
        {
            buildMenu.SetActive(!buildMenu.activeSelf);
        }
    }

    // Hàm gắn vào Tools_Btn ở thanh công cụ dưới
    public void OnClickToolsButton()
    {
        ExitActionModes();
        // Hiện bảng hướng dẫn thao tác (ví dụ hướng dẫn click chọn nhà để phá dỡ)
        if (controlHintsGroup != null) controlHintsGroup.SetActive(true);
        Debug.Log("Đã chọn bộ công cụ.");
    }

    // Hàm gắn vào Setting_Btn ở thanh công cụ dưới
    public void OnClickSettingButton()
    {
        ExitActionModes();
        if (settingUI != null)
        {
            settingUI.SetActive(!settingUI.activeSelf);
        }
    }

    // Hàm bổ trợ ẩn bảng hướng dẫn
    public void ExitActionModes()
    {
        if (controlHintsGroup != null) controlHintsGroup.SetActive(false);
    }

    // Hàm dùng để kích hoạt bảng hướng dẫn (gọi nội bộ khi chọn xong công trình)
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
    // Tự động bật bảng hướng dẫn điều khiển ngay khi người chơi chọn loại nhà

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