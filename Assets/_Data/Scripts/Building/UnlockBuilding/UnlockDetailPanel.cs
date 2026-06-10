using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
 * UnlockDetailPanel.cs
 * Quản lý Panel Popup hiển thị chi tiết điều kiện mở khóa tài nguyên.
 */
public class UnlockDetailPanel : MonoBehaviour
{
    public static UnlockDetailPanel Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private GameObject panelContainer;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Requirement Texts")]
    [SerializeField] private TMP_Text goldCostText;
    [SerializeField] private TMP_Text woodCostText;
    [SerializeField] private TMP_Text stoneCostText;
    [SerializeField] private TMP_Text foodCostText;

    [Header("Buttons")]
    [SerializeField] private Button unlockButton;
    [SerializeField] private Button closeButton;

    private UnlockableEntity _currentTarget;

    private void Awake()
    {
        Instance = this;
        if (panelContainer != null) panelContainer.SetActive(false);

        if (unlockButton != null) unlockButton.onClick.AddListener(OnClickUnlock);
        if (closeButton != null) closeButton.onClick.AddListener(HidePanel);
    }

    public void ShowPanel(UnlockableEntity entity)
    {
        if (entity == null) return;
        _currentTarget = entity;

        if (panelContainer != null) panelContainer.SetActive(true);

        // Đổ dữ liệu text thông tin
        if (titleText != null) titleText.text = entity.entityName;
        if (descriptionText != null) descriptionText.text = entity.entityDescription;

        // Đổ dữ liệu chi phí yêu cầu
        var req = entity.requirement;
        if (goldCostText != null) goldCostText.text = req.goldRequired.ToString();
        if (woodCostText != null) woodCostText.text = req.woodRequired.ToString();
        if (stoneCostText != null) stoneCostText.text = req.stoneRequired.ToString();
        if (foodCostText != null) foodCostText.text = req.foodRequired.ToString();

        // Kiểm tra tài nguyên runtime để bật/tắt độ sáng của nút mở khóa
        if (unlockButton != null)
        {
            unlockButton.interactable = entity.CanUnlock();
        }
    }

    private void OnClickUnlock()
    {
        if (_currentTarget == null) return;

        if (_currentTarget.ConfirmUnlock())
        {
            // Mở khóa thành công -> Đóng bảng luôn
            HidePanel();
        }
        else
        {
            if (UIManager.Ins != null)
            {
                UIManager.Ins.ShowWarning("Không đủ tài nguyên để mở khóa công trình!");
            }
        }
    }

    public void HidePanel()
    {
        _currentTarget = null;
        if (panelContainer != null) panelContainer.SetActive(false);
    }
}