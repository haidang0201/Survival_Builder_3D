using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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
    [SerializeField] private GameObject settingUI;            // Bảng cài đặt riêng biệt

    private Coroutine _fadeWarningCoroutine;

    void Start()
    {
        if (houseSelectionPanel != null) houseSelectionPanel.SetActive(true);
        if (workerStatusPanel != null) workerStatusPanel.SetActive(true);

        if (controlHintsGroup != null) controlHintsGroup.SetActive(false);
        if (settingUI != null) settingUI.SetActive(false);

        if (buildButton != null) buildButton.onClick.AddListener(ToggleBuildMenu);
        if (toolsButton != null) toolsButton.onClick.AddListener(OnClickToolsButton);
        if (settingButton != null) settingButton.onClick.AddListener(OnClickSettingButton);
    }

    void Update()
    {
        if (controlHintsGroup != null && controlHintsGroup.activeSelf)
        {
            if (Input.GetMouseButtonDown(1)) // Chuột phải
            {
                ExitActionModes();

                // Nếu có hàm cancel đặt trong BuildingSystem, bạn có thể gọi ở đây.
                // BuildingSystem.Ins.CancelPlacing();

                Debug.Log("Đã hủy chế độ xây dựng bằng Chuột Phải.");
            }
        }
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
        Debug.Log("Đã chọn bộ công cụ.");
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

    private void EnterPlacementMode()
    {
        if (controlHintsGroup != null) controlHintsGroup.SetActive(true);
        if (buildMenu != null) buildMenu.SetActive(false); // ẩn menu khi đang đặt nhà
    }

    // ================= WARNING UI =================

    public void ShowWarning(string message)
    {
        // Nếu bạn có Text hiển thị message trong warningUI, bạn cần add thêm tham chiếu Text.
        // Còn trong đoạn bạn gửi, mình chỉ bật panel.
        if (warningUI != null)
            warningUI.SetActive(true);

        if (_fadeWarningCoroutine != null)
        {
            StopCoroutine(_fadeWarningCoroutine);
            _fadeWarningCoroutine = null;
        }
    }

    public void HideWarning()
    {
        if (warningUI != null)
            warningUI.SetActive(false);

        if (_fadeWarningCoroutine != null)
        {
            StopCoroutine(_fadeWarningCoroutine);
            _fadeWarningCoroutine = null;
        }
    }

    private IEnumerator FadeOutWarning(float duration = 1.2f)
    {
        if (warningUI == null) yield break;

        // Nếu warningUI là Image/Text có alpha riêng thì bạn cần xử lý thêm.
        // Đoạn này chỉ tắt sau thời gian (an toàn nhất để khỏi phụ thuộc component).
        yield return new WaitForSeconds(duration);
        HideWarning();
    }

    // Nếu bạn vẫn muốn tự fade khi enter placement mode (giống ý code cũ),
    // bạn có thể gọi ShowWarning rồi bắt đầu FadeOutWarning tại nơi bạn muốn.
    // Ví dụ: ShowWarning("..."); _fadeWarningCoroutine = StartCoroutine(FadeOutWarning());

    // ================= BUILDING BUTTONS =================
    // Lưu ý: sửa enum theo BuildingType.cs bạn gửi (House, WoodCutter, StoneMine, Warehouse, Kitchen, ...)

    public void OnClickHouseButton()
    {
        EnterPlacementMode();
        BuildingSystem.Ins.StartPlacing(BuildingType.House);
    }

    public void OnClickWoodCutterButton()
    {
        EnterPlacementMode();
        // Không có ForestHut trong enum -> map tạm theo WoodCutter.
        BuildingSystem.Ins.StartPlacing(BuildingType.WoodCutter);
    }

    public void OnClickStoneStorageButton()
    {
        EnterPlacementMode();
        // Không có Sawmill trong enum -> map tạm theo WoodCutter.
        BuildingSystem.Ins.StartPlacing(BuildingType.StoneStorage);
    }

    public void OnClickFoodStorageButton()
    {
        EnterPlacementMode();
        BuildingSystem.Ins.StartPlacing(BuildingType.FoodStorage);
    }

    // public void OnClickHouseBuilderButton()
    // {
    //     EnterPlacementMode();
    //     BuildingSystem.Ins.StartPlacing(BuildingType.House);
    // }
}