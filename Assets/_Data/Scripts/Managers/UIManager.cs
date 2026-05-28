using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/*
 * UIManager.cs
 * Folder: Scripts/UI/
 * Dự án: KHẨN HOANG (PENTA DEV)
 * Người thực hiện: VŨ (Giao diện UI chính) + ĐĂNG (Đồng bộ luồng đóng mở Panel đặt nhà)
 *
 * NHIỆM VỤ: Quản lý HUD, Menu xây dựng, bảng Hints hướng dẫn phím tắt và tiếp nhận nút bấm xây nhà.
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
    [SerializeField] private GameObject controlHintsGroup; // Bảng hướng dẫn đặt/xoay nhà (R / ESC)
    [SerializeField] private GameObject settingUI;            // Bảng cài đặt riêng biệt

    private Coroutine _fadeWarningCoroutine;

    void Start()
    {
        // Khởi tạo trạng thái giao diện ban đầu khi vào game
        if (houseSelectionPanel != null) houseSelectionPanel.SetActive(true);
        if (workerStatusPanel != null) workerStatusPanel.SetActive(true);

        if (controlHintsGroup != null) controlHintsGroup.SetActive(false);
        if (settingUI != null) settingUI.SetActive(false);

        // Đăng ký sự kiện lắng nghe nút bấm từ Toolbar dưới cùng
        if (buildButton != null) buildButton.onClick.AddListener(ToggleBuildMenu);
        if (toolsButton != null) toolsButton.onClick.AddListener(OnClickToolsButton);
        if (settingButton != null) settingButton.onClick.AddListener(OnClickSettingButton);
    }

    void Update()
    {
        // [ĐÃ FIX TRIỆT ĐỂ]: Toàn bộ logic bắt nút Click chuột phải (Hủy chế độ xây) 
        // đã được giao hoàn toàn cho GhostBuilding tự xử lý qua hệ thống Input độc lập.
        // Không bắt phím tại đây để tránh lỗi NullReferenceException và xung đột ẩn UI trước Ghost.
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
        Debug.Log("[UIManager] Đã chọn bộ công cụ.");
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

    // ================= INTERACTION COUPLING (KẾT NỐI VỚI BUILDING SYSTEM) =================

    /// <summary>
    /// Kích hoạt giao diện chế độ đặt công trình (Ẩn Menu chính để tránh vướng, hiện bảng phím tắt).
    /// </summary>
    public void EnterPlacementMode()
    {
        if (controlHintsGroup != null) controlHintsGroup.SetActive(true);
        if (buildMenu != null) buildMenu.SetActive(false);
    }

    /// <summary>
    /// Kết thúc giao diện chế độ đặt công trình (Tắt bảng phím tắt, hiện lại Menu chính/HUD).
    /// </summary>
    public void ExitPlacementMode(bool shouldReopenMenu)
    {
        if (controlHintsGroup != null) controlHintsGroup.SetActive(false); // Luôn tắt bảng hướng dẫn (R/ESC)

        if (buildMenu != null)
        {
            buildMenu.SetActive(shouldReopenMenu); // Nhận lệnh tắt/mở linh hoạt từ hệ thống
        }
    }

    // ================= WARNING UI LOGIC =================

    public void ShowWarning(string message)
    {
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
        yield return new WaitForSeconds(duration);
        HideWarning();
    }

    // ================= ON CLICK BUTTONS (NÚT BẤM GIAO DIỆN XÂY NHÀ) =================

    public void OnClickHouseButton()
    {
        BuildingSystem.Ins.StartPlacing(BuildingType.House);
    }

    public void OnClickWoodCutterButton()
    {
        BuildingSystem.Ins.StartPlacing(BuildingType.WoodCutter);
    }

    public void OnClickStoneStorageButton()
    {
        BuildingSystem.Ins.StartPlacing(BuildingType.StoneMine);
    }

    public void OnClickFoodStorageButton()
    {
        BuildingSystem.Ins.StartPlacing(BuildingType.FoodStorage);
    }
}