using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// LandConqueredUI.cs
/// Người làm: ThanhNhan
///
/// Quản lý panel "Khai Hoang Thành Công" — hiện sau khi player đã trả đủ
/// tài nguyên và unlock vùng đất.
///
/// - Nút [Chiếm Đóng]: đóng panel
/// - Nút [Tha Đó]:     đóng panel
/// (cả 2 đều chỉ đóng vì vùng đất đã được unlock rồi)
///
/// Setup trong Unity Inspector:
///   1. Gán script này lên GameObject LandConqueredPanel
///   2. Kéo các Text: titleText, zoneNameText, descText
///   3. Kéo các Button: btnChiemDong, btnThaDo
///   4. Đảm bảo có CanvasGroup (tự tạo nếu thiếu)
/// </summary>
public class LandConqueredUI : MonoBehaviour
{
    [Header("Text References")]
    [Tooltip("Tiêu đề lớn — ví dụ: '🏆 Khai Hoang Thành Công!'")]
    public TextMeshProUGUI titleText;
    [Tooltip("Tên vùng đất vừa chiếm — được set tự động")]
    public TextMeshProUGUI zoneNameText;
    [Tooltip("Mô tả phụ — ví dụ: 'Vùng đất đã thuộc về lãnh thổ của bạn!'")]
    public TextMeshProUGUI descText;

    [Header("Buttons")]
    [Tooltip("Nút Chiếm Đóng — xác nhận, đóng panel")]
    public Button btnChiemDong;
    [Tooltip("Nút Tha Đó — huỷ, đóng panel")]
    public Button btnThaDo;

    [Header("Default Texts (tự điền nếu Text null)")]
    public string defaultTitle = "🏆 Khai Hoang Thành Công!";
    public string defaultDesc  = "Vùng đất đã thuộc về lãnh thổ của bạn!";

    [Header("Animation")]
    public float animDuration = 0.25f;

    // ─── Private ─────────────────────────────────────────────────────────────
    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private Coroutine _animCoroutine;

    // Nhận từ LandUnlockManager khi Show() được gọi
    private LandZone _targetZone;   // Vùng đất cần unlock
    private int _requiredWood;      // Số gỗ cần trừ

    // ─── Unity Lifecycle ─────────────────────────────────────────────────────

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        // Nút Chiếm Đóng
        if (btnChiemDong != null)
        {
            btnChiemDong.onClick.RemoveAllListeners();
            btnChiemDong.onClick.AddListener(OnChiemDong);
        }

        // Nút Tha Đó
        if (btnThaDo != null)
        {
            btnThaDo.onClick.RemoveAllListeners();
            btnThaDo.onClick.AddListener(OnThaDo);
        }

        // Đặt text mặc định nếu chưa có
        if (titleText != null && string.IsNullOrEmpty(titleText.text))
            titleText.text = defaultTitle;

        if (descText != null && string.IsNullOrEmpty(descText.text))
            descText.text = defaultDesc;

        // Animation mở
        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateOpen());
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi từ LandUnlockManager sau khi player bấm [Mở Khóa] và đủ điều kiện.
    /// Truyền vào tên vùng đất, reference LandZone và số gỗ cần trừ khi xác nhận.
    /// </summary>
    public void Show(string zoneName, LandZone zone, int requiredWood)
    {
        _targetZone  = zone;
        _requiredWood = requiredWood;

        if (zoneNameText != null)
            zoneNameText.text = zoneName;

        if (titleText != null && string.IsNullOrEmpty(titleText.text))
            titleText.text = defaultTitle;

        if (descText != null && string.IsNullOrEmpty(descText.text))
            descText.text = defaultDesc;

        gameObject.SetActive(true);
        Debug.Log($"[LandConqueredUI] Hiện panel cho vùng đất: {zoneName}");
    }

    // ─── Button Handlers ─────────────────────────────────────────────────────

    private void OnChiemDong()
    {
        // Trừ gỗ
        if (JsonDataManager.Ins != null)
        {
            JsonDataManager.Ins.AddWood(-_requiredWood);
            Debug.Log($"[LandConqueredUI] [Chiếm Đóng] Đã trừ {_requiredWood} gỗ. Còn lại: {JsonDataManager.Ins.wood}");
        }

        // Unlock vùng đất
        if (_targetZone != null)
        {
            _targetZone.UnlockLand();
            _targetZone.NotifyPanelClosed();
        }

        Debug.Log("[LandConqueredUI] Player chọn [Chiếm Đóng] → Unlock hoàn tất.");
        ClosePanel();
    }

    private void OnThaDo()
    {
        // Chỉ đóng panel — đất vẫn bị khóa, có thể mở lại
        if (_targetZone != null)
            _targetZone.NotifyPanelClosed();

        Debug.Log("[LandConqueredUI] Player chọn [Thả Đó] → Đóng panel, đất vẫn bị khóa.");
        ClosePanel();
    }

    // ─── Private ─────────────────────────────────────────────────────────────

    private void ClosePanel()
    {
        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateClose());
    }

    // ─── Animation Coroutines ─────────────────────────────────────────────────

    private IEnumerator AnimateOpen()
    {
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        if (_rectTransform != null) _rectTransform.localScale = Vector3.one * 0.85f;

        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / animDuration);
            _canvasGroup.alpha = t;
            if (_rectTransform != null)
                _rectTransform.localScale = Vector3.Lerp(Vector3.one * 0.85f, Vector3.one, t);
            yield return null;
        }

        _canvasGroup.alpha = 1f;
        if (_rectTransform != null) _rectTransform.localScale = Vector3.one;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
    }

    private IEnumerator AnimateClose()
    {
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        float startAlpha = _canvasGroup.alpha;
        Vector3 startScale = _rectTransform != null ? _rectTransform.localScale : Vector3.one;
        float elapsed = 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / animDuration);
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            if (_rectTransform != null)
                _rectTransform.localScale = Vector3.Lerp(startScale, Vector3.one * 0.85f, t);
            yield return null;
        }

        gameObject.SetActive(false);
    }
}
