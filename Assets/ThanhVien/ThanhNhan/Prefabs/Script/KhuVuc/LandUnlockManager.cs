using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// LandUnlockManager.cs
/// Người làm: ThanhNhan
///
/// Quản lý panel mở khóa vùng đất mới dựa trên TÀI NGUYÊN (Gỗ + Worker).
/// - Đọc config từ building_config.json (entry "LandZone") nếu có
/// - Kiểm tra wood và worker hiện tại của player
/// - Khi đủ: cho phép click [Khai Hoang] → hiện LandConqueredPanel
/// - Việc trừ gỗ + unlock thực sự do LandConqueredUI xử lý khi bấm [Chiếm Đóng]
///
/// Setup trong Unity Inspector:
///   1. Gán script này lên GameObject LandUnlockPanel
///   2. Kéo các field UI vào: cardUI, confirmButton, cancelButton, backgroundButton
///   3. Kéo LandConqueredPanel vào field landConqueredPanel
///   4. Đặt requiredWood và requiredWorkers theo ý muốn
/// </summary>
public class LandUnlockManager : MonoBehaviour
{
    [Header("Panel References")]
    public ResourceCardUI cardUI;
    public Button confirmButton;     // Nút "Khai Hoang"
    public Button cancelButton;      // Nút "Bỏ Qua"
    public Button backgroundButton;  // Overlay trong suốt — click ngoài để đóng

    [Header("Conquered Panel")]
    [Tooltip("Kéo LandConqueredPanel vào đây — hiện sau khi khai hoang thành công")]
    public GameObject landConqueredPanel;

    [Header("Unlock Requirements")]
    [Tooltip("Số gỗ cần để khai hoang — tự đặt")]
    public int requiredWood = 50;
    [Tooltip("Số worker cần để khai hoang — tự đặt")]
    public int requiredWorkers = 2;
    [Tooltip("Fallback max worker nếu không đọc được save JSON")]
    public int maxWorkersFallback = 4;

    [Header("[DEBUG]")]
    [Tooltip("-1 = đọc từ save JSON | ≥ 0 = giả lập số worker để test")]
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

    // ─── Inner classes để đọc building_config.json ───────────────────────────
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

    // ─── Public API ──────────────────────────────────────────────────────────

    /// <summary>Bind LandZone trước khi bật panel.</summary>
    public void BindTargetZone(LandZone zone) => _targetZone = zone;

    /// <summary>Refresh toàn bộ UI dựa vào tài nguyên hiện tại của player.</summary>
    public void RefreshPanelData()
    {
        int currentWood = (JsonDataManager.Ins != null) ? JsonDataManager.Ins.wood : 0;

        int currentWorkers, maxWorkers;
        ReadWorkersFromSave(out currentWorkers, out maxWorkers);

        if (debugWorkerOverride >= 0)
        {
            currentWorkers = debugWorkerOverride;
            maxWorkers = maxWorkersFallback;
            Debug.LogWarning($"[LandUnlockManager] ⚠️ DEBUG: currentWorkers={debugWorkerOverride}");
        }

        bool enoughWood    = currentWood    >= requiredWood;
        bool enoughWorkers = currentWorkers >= requiredWorkers;
        bool canUnlock     = enoughWood && enoughWorkers;

        // Dùng lại ResourceCardUI — productionRate = -1 để ẩn dòng sản lượng
        if (cardUI != null)
            cardUI.SetResourceUnlockData(currentWood, requiredWood, -1, canUnlock,
                                         currentWorkers, maxWorkers, enoughWorkers);

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

    /// <summary>
    /// Đọc config LandZone từ StreamingAssets/building_config.json.
    /// Entry cần có buildingType = "LandZone".
    /// </summary>
    private void LoadConfigFromJson()
    {
        if (debugIgnoreJsonConfig)
        {
            Debug.LogWarning("[LandUnlockManager] ⚠️ DEBUG: Bỏ qua JSON, dùng giá trị Inspector.");
            return;
        }

        string configPath = Path.Combine(Application.streamingAssetsPath, "building_config.json");
        if (!File.Exists(configPath))
        {
            Debug.LogWarning("[LandUnlockManager] Không tìm thấy building_config.json. Dùng giá trị mặc định.");
            return;
        }

        try
        {
            string json = File.ReadAllText(configPath);
            var root = JsonUtility.FromJson<LandConfigRoot>(json);
            if (root == null || root.buildingConfigs == null) return;

            var entry = root.buildingConfigs.Find(c => c.buildingType == "LandZone");
            if (entry == null)
            {
                Debug.LogWarning("[LandUnlockManager] Không tìm thấy entry 'LandZone' trong building_config.json.");
                return;
            }

            if (entry.requiredWood    > 0) requiredWood    = entry.requiredWood;
            if (entry.requiredWorkers > 0) requiredWorkers = entry.requiredWorkers;

            Debug.Log($"[LandUnlockManager] Config loaded → Gỗ: {requiredWood}, Worker: {requiredWorkers}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[LandUnlockManager] Lỗi đọc config JSON: " + ex.Message);
        }
    }

    /// <summary>
    /// Đọc số worker hiện tại từ save JSON (builder.json).
    /// Lấy worker từ StoneMine — hoặc building đầu tiên có worker trong save.
    /// </summary>
    private void ReadWorkersFromSave(out int currentWorkers, out int maxWorkers)
    {
        currentWorkers = 0;
        maxWorkers = maxWorkersFallback;

        if (JsonDataManager.Ins == null) return;

        string savePath = Path.Combine(Application.persistentDataPath, JsonDataManager.Ins.saveFileName);
        if (!File.Exists(savePath)) return;

        try
        {
            string json = File.ReadAllText(savePath);
            var save = JsonUtility.FromJson<JsonDataManager.GameSaveData>(json);
            if (save == null || save.buildings == null) return;

            // Tìm từ cuối danh sách (entry mới nhất)
            for (int i = save.buildings.Count - 1; i >= 0; i--)
            {
                var state = save.buildings[i];
                if (state == null) continue;

                currentWorkers = Mathf.Max(0, state.currentWorkers);
                maxWorkers     = state.maxWorkers > 0 ? state.maxWorkers : maxWorkersFallback;
                return;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[LandUnlockManager] Không đọc được save JSON: " + ex.Message);
        }
    }

    private void OnClickUnlock()
    {
        if (confirmButton != null && !confirmButton.interactable) return;

        // KHÔNG trừ gỗ và KHÔNG unlock ở đây!
        // → Việc trừ gỗ + unlock sẽ do LandConqueredUI xử lý khi player bấm [Chiếm Đóng]
        // → Nếu player bấm [Thả Đó], đất vẫn bị khóa và có thể mở lại

        // Tắt overlay
        if (backgroundButton != null)
            backgroundButton.gameObject.SetActive(false);

        // Đóng panel này, rồi hiện LandConqueredPanel
        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateCloseAndShowConquered());
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

    /// <summary>Đóng panel unlock → chờ animation xong → bật LandConqueredPanel.</summary>
    private IEnumerator AnimateCloseAndShowConquered()
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

        // Hiện panel thành công — truyền cả zone và requiredWood để LandConqueredUI xử lý
        if (landConqueredPanel != null)
        {
            string zoneName = _targetZone != null ? _targetZone.gameObject.name : "Vùng Đất Mới";
            var conqueredUI = landConqueredPanel.GetComponent<LandConqueredUI>();
            if (conqueredUI != null)
                conqueredUI.Show(zoneName, _targetZone, requiredWood);
            else
                landConqueredPanel.SetActive(true);
        }
    }
}
