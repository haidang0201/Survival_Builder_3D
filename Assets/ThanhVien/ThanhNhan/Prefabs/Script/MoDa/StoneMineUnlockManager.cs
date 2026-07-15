using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// StoneMineUnlockManager.cs
/// Người làm: ThanhNhan
///
/// Quản lý panel mở khóa mỏ đá dựa trên TÀI NGUYÊN (gỗ).
/// - Đọc config yêu cầu gỗ từ building_config.json (StreamingAssets)
/// - Kiểm tra JsonDataManager.Ins.wood đủ chưa
/// - Khi đủ: cho phép click MỞ KHÓA → trừ gỗ → unlock mỏ đá
///
/// Setup trong Unity Inspector:
///   1. Gán script này lên GameObject MoDaCard
///   2. Kéo CardUI, ConfirmButton, BackgroundButton vào
///   3. ResourceNode → kéo MoDaCard vào field moDaCardPanel
/// </summary>
public class StoneMineUnlockManager : MonoBehaviour
{
    [Header("Panel References")]
    public ResourceCardUI cardUI;
    public Button confirmButton;
    public Button backgroundButton;   // Overlay đóng panel khi click ngoài

    [Header("Unlock Requirements (fallback nếu không đọc được JSON)")]
    [Tooltip("Số gỗ cần để mở khóa mỏ đá")]
    public int requiredWood = 100;

    [Header("Production Info")]
    [Tooltip("Sản lượng đá/phút – sẽ bị ghi đè bởi building_config.json nếu có")]
    public int productionRatePerMinute = 6;

    [Header("Worker Info")]
    [Tooltip("Số worker yêu cầu – fallback nếu không có trong JSON")]
    public int requiredWorkers = 4;
    [Tooltip("Số worker tối đa – fallback nếu không có trong save JSON")]
    public int maxWorkersFallback = 4;

    [Header("[DEBUG] Chỉ dùng khi test, đặt lại -1 trước khi build")]
    [Tooltip("-1 = đọc từ save JSON bình thường | ≥ 0 = giả lập số worker hiện tại để test")]
    public int debugWorkerOverride = -1;
    [Tooltip("Nếu bật, bỏ qua building_config.json và dùng đúng giá trị Inspector")]
    public bool debugIgnoreJsonConfig = false;

    [Header("Animation")]
    [Tooltip("Thời gian hiệu ứng mở/đóng (giây)")]
    public float animDuration = 0.2f;

    // ─── Private ────────────────────────────────────────────────────────────
    private ResourceNode _targetNode;
    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private Coroutine _animCoroutine;

    // ─── Inner classes để đọc building_config.json (độc lập với JsonDataManager) ─
    [Serializable]
    private class StoneMineConfigRoot
    {
        public List<StoneMineConfigEntry> buildingConfigs;
    }

    [Serializable]
    private class StoneMineConfigEntry
    {
        public string buildingType;
        public int requiredWood;
        public int requiredWorkers;
        public int productionPerMinute;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Unity Lifecycle
    // ────────────────────────────────────────────────────────────────────────

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
        // Gán listener nút mở khóa
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnClickUnlock);
        }

        // Gán listener background (click ngoài để đóng)
        if (backgroundButton != null)
        {
            backgroundButton.gameObject.SetActive(true);
            backgroundButton.onClick.RemoveAllListeners();
            backgroundButton.onClick.AddListener(ClosePanel);
        }

        // Load config JSON → Refresh UI
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

    // ────────────────────────────────────────────────────────────────────────
    // Public API (gọi từ ResourceNode)
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>Bind ResourceNode trước khi bật panel.</summary>
    public void BindTargetNode(ResourceNode node) => _targetNode = node;

    /// <summary>Refresh toàn bộ UI dựa vào tài nguyên hiện tại của player.</summary>
    public void RefreshPanelData()
    {
        // Đọc gỗ hiện tại
        int currentWood = (JsonDataManager.Ins != null) ? JsonDataManager.Ins.wood : 0;

        // Đọc worker hiện tại từ save JSON
        int currentWorkers, maxWorkers;
        ReadWorkersFromSave(out currentWorkers, out maxWorkers);

        // [DEBUG] Nếu debugWorkerOverride >= 0, dùng giá trị debug thay vì save
        if (debugWorkerOverride >= 0)
        {
            currentWorkers = debugWorkerOverride;
            maxWorkers = maxWorkersFallback;
            Debug.LogWarning($"[StoneMineUnlockManager] ⚠️ DEBUG MODE: currentWorkers={debugWorkerOverride} (không đọc save)");
        }

        // Điều kiện mở khóa: Đủ GỘ và Đủ WORKER
        bool enoughWood    = currentWood    >= requiredWood;
        bool enoughWorkers = currentWorkers >= requiredWorkers;
        bool canUnlock     = enoughWood && enoughWorkers;

        if (cardUI != null)
            cardUI.SetResourceUnlockData(currentWood, requiredWood, productionRatePerMinute, canUnlock,
                                         currentWorkers, maxWorkers, enoughWorkers);

        if (confirmButton != null)
            confirmButton.interactable = canUnlock;
    }

    /// <summary>Đóng panel — gọi từ bên ngoài hoặc nút background.</summary>
    public void ClosePanel()
    {
        // Tắt overlay ngay để không chặn click vào mỏ đá nữa
        if (backgroundButton != null)
            backgroundButton.gameObject.SetActive(false);

        // Thông báo ResourceNode kích hoạt cooldown
        if (_targetNode != null)
            _targetNode.NotifyPanelClosed();

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateClose());
    }

    // ────────────────────────────────────────────────────────────────────────
    // Private Logic
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Đọc config StoneMine trực tiếp từ StreamingAssets/building_config.json.
    /// Không phụ thuộc vào hàm helper của JsonDataManager.
    /// </summary>
    private void LoadConfigFromJson()
    {
        // [DEBUG] Bỏ qua JSON, dùng thẳng giá trị Inspector
        if (debugIgnoreJsonConfig)
        {
            Debug.LogWarning("[StoneMineUnlockManager] ⚠️ DEBUG: Bỏ qua JSON config, dùng giá trị Inspector.");
            return;
        }

        string configPath = Path.Combine(Application.streamingAssetsPath, "building_config.json");

        if (!File.Exists(configPath))
        {
            Debug.LogWarning("[StoneMineUnlockManager] Không tìm thấy building_config.json. Dùng giá trị mặc định.");
            return;
        }


        try
        {
            string json = File.ReadAllText(configPath);
            var root = JsonUtility.FromJson<StoneMineConfigRoot>(json);

            if (root == null || root.buildingConfigs == null) return;

            var entry = root.buildingConfigs.Find(c => c.buildingType == "StoneMine");
            if (entry == null)
            {
                Debug.LogWarning("[StoneMineUnlockManager] Không tìm thấy entry 'StoneMine' trong building_config.json.");
                return;
            }

            if (entry.requiredWood > 0)
                requiredWood = entry.requiredWood;

            if (entry.requiredWorkers > 0)
                requiredWorkers = entry.requiredWorkers;

            if (entry.productionPerMinute > 0)
                productionRatePerMinute = entry.productionPerMinute;

            Debug.Log($"[StoneMineUnlockManager] Config loaded → Gỗ cần: {requiredWood}, Worker cần: {requiredWorkers}, Sản lượng: {productionRatePerMinute}/phút");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[StoneMineUnlockManager] Lỗi đọc config JSON: " + ex.Message);
        }
    }

    /// <summary>
    /// Đọc số worker hiện tại của StoneMine từ builder.json (save game).
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
                if (state == null || state.buildingType != BuildingType.StoneMine) continue;

                currentWorkers = Mathf.Max(0, state.currentWorkers);
                maxWorkers = state.maxWorkers > 0 ? state.maxWorkers : maxWorkersFallback;
                return;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[StoneMineUnlockManager] Không đọc được save JSON: " + ex.Message);
        }
    }

    private void OnClickUnlock()
    {
        if (confirmButton != null && !confirmButton.interactable)
        {
            Debug.Log("[StoneMineUnlockManager] Chưa đủ gỗ để mở khóa.");
            return;
        }

        // Trừ gỗ
        if (JsonDataManager.Ins != null)
        {
            JsonDataManager.Ins.AddWood(-requiredWood);
            Debug.Log($"[StoneMineUnlockManager] Đã trừ {requiredWood} gỗ. Còn lại: {JsonDataManager.Ins.wood}");
        }

        // Unlock mỏ đá
        if (_targetNode != null)
            _targetNode.UnlockNode();

        ClosePanel();
    }

    // ────────────────────────────────────────────────────────────────────────
    // Animation Coroutines
    // ────────────────────────────────────────────────────────────────────────

    private IEnumerator AnimateOpen()
    {
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
