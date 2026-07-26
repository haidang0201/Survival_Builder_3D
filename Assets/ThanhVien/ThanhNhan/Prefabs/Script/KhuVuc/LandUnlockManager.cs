using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/*
 * LandUnlockManager.cs
 * Người làm: ThanhNhan
 * Người tối ưu Real-time: VŨ
 *
 * Quản lý panel mở khóa vùng đất mới dựa trên TÀI NGUYÊN (Gỗ) và DÂN SỐ thực tế (Tag Worker).
 * - Đọc config từ building_config.json (entry "LandZone") nếu có
 * - Đếm trực tiếp số Worker thực tế đang có trên Scene
 * - Cập nhật UI thời gian thực mỗi 0.3 giây
 */
public class LandUnlockManager : MonoBehaviour
{
    [Header("Panel References")]
    public ResourceCardUI cardUI;
    public Button confirmButton;     // Nút "Khai Hoang"
    public Button cancelButton;      // Nút "Bỏ Qua"
    public Button backgroundButton;  // Overlay trong suốt — click ngoài để đóng

    // [Header("Conquered Panel")]
    // [Tooltip("Kéo LandConqueredPanel vào đây — hiện sau khi khai hoang thành công")]
    // public GameObject landConqueredPanel;

    [Header("Unlock Requirements")]
    [Tooltip("Số gỗ cần để khai hoang — tự đặt hoặc đọc từ JSON")]
    public int requiredWood = 50;
    [Tooltip("Số worker thực tế cần có trên Scene để khai hoang")]
    public int requiredWorkers = 2;

    [Header("Worker Tag Configuration")]
    [Tooltip("Tag chính xác của các nhân vật Worker trên Scene")]
    public string workerTag = "Worker";

    [Header("[DEBUG]")]
    [Tooltip("-1 = chạy realtime | ≥ 0 = giả lập số worker để test")]
    public int debugWorkerOverride = -1;
    [Tooltip("Bỏ qua building_config.json, chỉ dùng giá trị Inspector")]
    public bool debugIgnoreJsonConfig = false;

    [Header("Animation")]
    public float animDuration = 0.2f;

    // ─── Private ─────────────────────────────────────────────────────────────
    private LandZone _targetZone;
    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private Coroutine _animCoroutine;
    private float _nextUpdateTime = 0f; // Bộ đếm thời gian quét Real-time

    [Serializable]
    private class LandConfigRoot
    {
        public List<LandConfigEntry> buildingConfigs;
    }

    [Serializable]
    private class LandConfigEntry
    {
        public string buildingType;
        public int requiredWood;
        public int requiredWorkers;
    }

    // ─── Unity Lifecycle ─────────────────────────────────────────────────────

    private void Awake()
    {
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
        // Nút Khai Hoang
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnClickUnlock);
        }

        // Nút Bỏ Qua
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(ClosePanel);
        }

        // Background overlay
        if (backgroundButton != null)
        {
            backgroundButton.gameObject.SetActive(true);
            backgroundButton.onClick.RemoveAllListeners();
            backgroundButton.onClick.AddListener(ClosePanel);
        }

        // Load config → refresh UI
        LoadConfigFromJson();
        RefreshPanelData();

        // Animation mở
        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateOpen());
    }

    private void OnDisable()
    {
        if (backgroundButton != null)
            backgroundButton.gameObject.SetActive(false);
    }

    // 🌟 THỜI GIAN THỰC: Tự động đếm lại Tag mỗi 0.3 giây khi Panel đang bật
    private void Update()
    {
        if (Time.time >= _nextUpdateTime)
        {
            _nextUpdateTime = Time.time + 0.3f;
            RefreshPanelData();
        }
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    /// <summary>Bind LandZone trước khi bật panel.</summary>
    public void BindTargetZone(LandZone zone) => _targetZone = zone;

    /// <summary>Refresh toàn bộ UI dựa trên tài nguyên và số lượng Worker thực tế.</summary>
    public void RefreshPanelData()
    {
        // 1. Lấy gỗ hiện tại từ RAM
        int currentWood = (JsonDataManager.Ins != null) ? JsonDataManager.Ins.wood : 0;

        // 2. ĐẾM TRỰC TIẾP WORKER TRÊN SCENE BẰNG TAG
        int currentWorkers = GameObject.FindGameObjectsWithTag(workerTag).Length;

        if (debugWorkerOverride >= 0)
        {
            currentWorkers = debugWorkerOverride;
            Debug.LogWarning($"[LandUnlockManager] ⚠️ DEBUG: currentWorkers={debugWorkerOverride}");
        }

        // 3. Đánh giá điều kiện
        bool enoughWood    = currentWood    >= requiredWood;
        bool enoughWorkers = currentWorkers >= requiredWorkers;
        bool canUnlock     = enoughWood && enoughWorkers;

        // 4. Đổ dữ liệu lên UI:
        // - Truyền requiredWorkers vào vị trí maxWorkers để UI hiển thị dạng "Hiện có/Yêu cầu" (Ví dụ: 1/2)
        if (cardUI != null)
        {
            cardUI.SetResourceUnlockData(
                currentWood, 
                requiredWood, 
                -1, // productionRate = -1 để tự động ẩn dòng sản lượng đá/phút
                canUnlock,
                currentWorkers, 
                requiredWorkers, 
                enoughWorkers
            );
        }

        if (confirmButton != null)
            confirmButton.interactable = canUnlock;
    }

    /// <summary>Đóng panel — gọi từ nút Bỏ Qua hoặc background overlay.</summary>
    public void ClosePanel()
    {
        if (backgroundButton != null)
            backgroundButton.gameObject.SetActive(false);

        if (_targetZone != null)
            _targetZone.NotifyPanelClosed();

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateClose());
    }

    // ─── Private Logic ───────────────────────────────────────────────────────

    private void LoadConfigFromJson()
    {
        if (debugIgnoreJsonConfig) return;

        string configPath = Path.Combine(Application.streamingAssetsPath, "building_config.json");
        if (!File.Exists(configPath)) return;

        try
        {
            string json = File.ReadAllText(configPath);
            var root = JsonUtility.FromJson<LandConfigRoot>(json);
            if (root == null || root.buildingConfigs == null) return;

            var entry = root.buildingConfigs.Find(c => c.buildingType == "LandZone");
            if (entry == null) return;

            if (entry.requiredWood    > 0) requiredWood    = entry.requiredWood;
            if (entry.requiredWorkers > 0) requiredWorkers = entry.requiredWorkers;

            Debug.Log($"[LandUnlockManager] Config loaded → Gỗ: {requiredWood}, Worker: {requiredWorkers}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[LandUnlockManager] Lỗi đọc config JSON: " + ex.Message);
        }
    }

    private void OnClickUnlock()
    {
        if (confirmButton != null && !confirmButton.interactable) return;

        // Tắt overlay
        if (backgroundButton != null)
            backgroundButton.gameObject.SetActive(false);

        // Đóng panel này, rồi hiện LandConqueredPanel
        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        // SỬA Ở ĐÂY: Dùng AnimateClose() thay vì AnimateCloseAndShowConquered()
        _animCoroutine = StartCoroutine(AnimateClose());
        // _animCoroutine = StartCoroutine(AnimateCloseAndShowConquered());
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

    // private IEnumerator AnimateCloseAndShowConquered()
    // {
    //     _canvasGroup.interactable = false;
    //     _canvasGroup.blocksRaycasts = false;

    //     float startAlpha = _canvasGroup.alpha;
    //     Vector3 startScale = _rectTransform != null ? _rectTransform.localScale : Vector3.one;
    //     float elapsed = 0f;

    //     while (elapsed < animDuration)
    //     {
    //         elapsed += Time.unscaledDeltaTime;
    //         float t = Mathf.SmoothStep(0f, 1f, elapsed / animDuration);
    //         _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
    //         if (_rectTransform != null)
    //             _rectTransform.localScale = Vector3.Lerp(startScale, Vector3.one * 0.85f, t);
    //         yield return null;
    //     }

    //     gameObject.SetActive(false);

    //     if (landConqueredPanel != null)
    //     {
    //         string zoneName = _targetZone != null ? _targetZone.gameObject.name : "Vùng Đất Mới";
    //         var conqueredUI = landConqueredPanel.GetComponent<LandConqueredUI>();
    //         if (conqueredUI != null)
    //             conqueredUI.Show(zoneName, _targetZone, requiredWood);
    //         else
    //             landConqueredPanel.SetActive(true);
    //     }
    // }
}