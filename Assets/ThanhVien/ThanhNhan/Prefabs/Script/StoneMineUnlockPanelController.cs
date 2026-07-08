using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;

public class StoneMineUnlockPanelController : MonoBehaviour
{
    [Header("Panel References")]
    public ResourceCardUI cardUI;
    public Button confirmButton;
    public Button backgroundButton; // Overlay/background để đóng panel khi bấm ngoài

    [Header("Logic Config")]
    public BuildingType targetBuildingType = BuildingType.StoneMine;
    public int requiredWorkersToUnlock = 4;
    public int productionRatePerMinute = 6;

    [Header("Animation")]
    [Tooltip("Thời gian hiệu ứng mở/đóng (giây)")]
    public float animDuration = 0.2f;

    private ResourceNode targetNode;
    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private Coroutine _animCoroutine;

    private void Awake()
    {
        // Cache components
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _rectTransform = GetComponent<RectTransform>();

        if (cardUI == null)
            cardUI = GetComponent<ResourceCardUI>();

        if (confirmButton == null && cardUI != null)
            confirmButton = cardUI.confirmButton;
    }

    private void OnEnable()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnClickUnlock);
        }

        if (backgroundButton != null)
        {
            backgroundButton.gameObject.SetActive(true);
            backgroundButton.onClick.RemoveAllListeners();
            backgroundButton.onClick.AddListener(ClosePanel);
        }

        RefreshPanelData();

        // Chạy animation mở
        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateOpen());
    }

    private void OnDisable()
    {
        if (backgroundButton != null)
            backgroundButton.gameObject.SetActive(false);
    }

    public void BindTargetNode(ResourceNode node)
    {
        targetNode = node;
    }

    public void RefreshPanelData()
    {
        int currentWorkers;
        int maxWorkers;
        ReadWorkersFromSave(out currentWorkers, out maxWorkers);

        bool canUnlock = currentWorkers >= requiredWorkersToUnlock;

        if (cardUI != null)
            cardUI.SetUIData(currentWorkers, requiredWorkersToUnlock, productionRatePerMinute, currentWorkers, maxWorkers, canUnlock);

        if (confirmButton != null)
            confirmButton.interactable = canUnlock;
    }

    private void ReadWorkersFromSave(out int currentWorkers, out int maxWorkers)
    {
        currentWorkers = 0;
        maxWorkers = requiredWorkersToUnlock;

        if (JsonDataManager.Ins == null) return;

        string savePath = Path.Combine(Application.persistentDataPath, JsonDataManager.Ins.saveFileName);
        if (!File.Exists(savePath)) return;

        try
        {
            string json = File.ReadAllText(savePath);
            var save = JsonUtility.FromJson<JsonDataManager.GameSaveData>(json);
            if (save == null || save.buildings == null) return;

            for (int i = save.buildings.Count - 1; i >= 0; i--)
            {
                var state = save.buildings[i];
                if (state == null || state.buildingType != targetBuildingType) continue;

                currentWorkers = Mathf.Max(0, state.currentWorkers);
                maxWorkers = state.maxWorkers > 0 ? state.maxWorkers : requiredWorkersToUnlock;
                return;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[StoneMineUnlockPanelController] Không đọc được save JSON: " + ex.Message);
        }
    }

    private void OnClickUnlock()
    {
        if (confirmButton != null && !confirmButton.interactable)
        {
            Debug.Log("Chưa đủ worker để mở khóa mỏ đá.");
            return;
        }

        if (targetNode != null)
            targetNode.UnlockNode();

        ClosePanel();
    }

    public void ClosePanel()
    {
        // Tắt overlay NGAY LẬP TỨC để không chặn click vào mỏ đá nữa
        if (backgroundButton != null)
            backgroundButton.gameObject.SetActive(false);

        // Thông báo ResourceNode ngay để kích hoạt cooldown
        if (targetNode != null)
            targetNode.NotifyPanelClosed();

        // Ngăn double-call khi coroutine đang chạy
        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateClose());
    }

    // ─── Animation Coroutines ────────────────────────────────────────────────

    private IEnumerator AnimateOpen()
    {
        // Đặt trạng thái ban đầu: trong suốt + nhỏ hơn một chút
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        if (_rectTransform != null)
            _rectTransform.localScale = Vector3.one * 0.85f;

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

        // Đảm bảo kết thúc đúng giá trị
        _canvasGroup.alpha = 1f;
        if (_rectTransform != null) _rectTransform.localScale = Vector3.one;

        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
    }

    private IEnumerator AnimateClose()
    {
        // backgroundButton đã bị tắt từ ClosePanel() rồi
        // Chỉ cần khóa input để tránh double-click vào các nút trong panel
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
