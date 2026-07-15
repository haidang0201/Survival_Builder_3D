using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/*
 * StoneMineUnlockManager.cs
 * Dự án: KHẨN HOANG (PENTA DEV)
 * Người thực hiện tối ưu: VŨ
 * Tính năng: Quét Tag "Worker" thời gian thực để làm điều kiện mở khóa mỏ đá.
 */

public class StoneMineUnlockManager : MonoBehaviour
{
    [Header("Panel References")]
    public ResourceCardUI cardUI;
    public Button confirmButton;
    public Button backgroundButton; 

    [Header("Unlock Requirements (fallback nếu không đọc được JSON)")]
    [Tooltip("Số gỗ cần để mở khóa")]
    public int requiredWood = 100;
    [Tooltip("Số lượng Worker cần có trong Scene để mở khóa")]
    public int requiredWorkers = 4;

    [Header("Production Info")]
    public int productionRatePerMinute = 6;

    [Header("Worker Tag Configuration")]
    [Tooltip("Tag chính xác của các nhân vật Worker trên Scene")]
    public string workerTag = "Worker";

    [Header("[DEBUG] Đặt lại -1 trước khi build")]
    public int debugWorkerOverride = -1;
    public bool debugIgnoreJsonConfig = false;

    [Header("Animation")]
    public float animDuration = 0.2f;

    // ─── Private ────────────────────────────────────────────────────────────
    private ResourceNode _targetNode;
    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private Coroutine _animCoroutine;
    private float _nextUpdateTime = 0f;

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

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _rectTransform = GetComponent<RectTransform>();

        if (cardUI == null) cardUI = GetComponent<ResourceCardUI>();
        if (confirmButton == null && cardUI != null) confirmButton = cardUI.confirmButton;
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

        LoadConfigFromJson();
        RefreshPanelData();

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateOpen());
    }

    private void OnDisable()
    {
        if (backgroundButton != null)
            backgroundButton.gameObject.SetActive(false);
    }

    // Cập nhật liên tục số lượng Worker ngoài Scene mỗi 0.3 giây
    private void Update()
    {
        if (Time.time >= _nextUpdateTime)
        {
            _nextUpdateTime = Time.time + 0.3f;
            RefreshPanelData();
        }
    }

    public void BindTargetNode(ResourceNode node) => _targetNode = node;

    /// <summary>
    /// Đếm trực tiếp số Worker trên Scene và hiển thị lên UI
    /// </summary>
    public void RefreshPanelData()
    {
        // 1. Đọc gỗ hiện tại của người chơi từ file Save
        int currentWood = (JsonDataManager.Ins != null) ? JsonDataManager.Ins.wood : 0;

        // 2. ĐẾM THẲNG SỐ LƯỢNG WORKER ĐANG CÓ TRÊN SCENE BẰNG TAG
        int currentWorkers = GameObject.FindGameObjectsWithTag(workerTag).Length;

        // Chế độ debug nếu cần test nhanh trên Inspector
        if (debugWorkerOverride >= 0)
        {
            currentWorkers = debugWorkerOverride;
        }

        // 3. Đánh giá điều kiện mở khóa
        bool enoughWood = currentWood >= requiredWood;
        bool enoughWorkers = currentWorkers >= requiredWorkers; // So sánh với số lượng yêu cầu
        bool canUnlock = enoughWood && enoughWorkers;

        // 4. Đổ dữ liệu lên UI:
        // - Tham số thứ 5 (currentWorkers): Số lượng Worker hiện có trên Scene
        // - Tham số thứ 6 (maxWorkers): Đổi thành requiredWorkers để UI hiển thị dạng "[Có]/[Cần]" (Ví dụ: 3/4)
        if (cardUI != null)
        {
            cardUI.SetResourceUnlockData(
                currentWood, 
                requiredWood, 
                productionRatePerMinute, 
                canUnlock,
                currentWorkers, 
                requiredWorkers, 
                enoughWorkers
            );
        }

        if (confirmButton != null)
        {
            confirmButton.interactable = canUnlock;
        }
    }

    private void OnClickUnlock()
    {
        if (confirmButton != null && !confirmButton.interactable) return;

        // Trừ gỗ lưu vào JSON
        if (JsonDataManager.Ins != null)
        {
            JsonDataManager.Ins.AddWood(-requiredWood);
        }

        // Kích hoạt mở khóa mỏ đá trên Scene
        if (_targetNode != null)
            _targetNode.UnlockNode();

        ClosePanel();
    }

    public void ClosePanel()
    {
        if (backgroundButton != null)
            backgroundButton.gameObject.SetActive(false);

        if (_targetNode != null)
            _targetNode.NotifyPanelClosed();

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateClose());
    }

    private void LoadConfigFromJson()
    {
        if (debugIgnoreJsonConfig) return;

        string configPath = Path.Combine(Application.streamingAssetsPath, "building_config.json");
        if (!File.Exists(configPath)) return;

        try
        {
            string json = File.ReadAllText(configPath);
            var root = JsonUtility.FromJson<StoneMineConfigRoot>(json);
            if (root == null || root.buildingConfigs == null) return;

            var entry = root.buildingConfigs.Find(c => c.buildingType == "StoneMine");
            if (entry == null) return;

            if (entry.requiredWood > 0) requiredWood = entry.requiredWood;
            if (entry.requiredWorkers > 0) requiredWorkers = entry.requiredWorkers;
            if (entry.productionPerMinute > 0) productionRatePerMinute = entry.productionPerMinute;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[StoneMineUnlockManager] Lỗi đọc config JSON: " + ex.Message);
        }
    }

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
            if (_rectTransform != null) _rectTransform.localScale = Vector3.Lerp(Vector3.one * 0.85f, Vector3.one, t);
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
            if (_rectTransform != null) _rectTransform.localScale = Vector3.Lerp(startScale, Vector3.one * 0.85f, t);
            yield return null;
        }

        gameObject.SetActive(false);
    }
}