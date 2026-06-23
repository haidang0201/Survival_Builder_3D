using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
 * BuildingProgressBarUI.cs
 * Folder: Scripts/UI/
 * Dự án: KHẨN HOANG (PENTA DEV)
 * Người thực hiện chỉnh sửa: VŨ (Luồng UI)
 * * GIẢI QUYẾT TRIỆT ĐỂ: Biến AudioSource thành mảng có cộng trừ độc lập cho từng luồng VFX.
 * CHỈ SỬA DUY NHẤT FILE NÀY – AN TOÀN TUYỆT ĐỐI CHO HỆ THỐNG GỐC CỦA NHÓM.
 */
public class BuildingProgressBarUI : MonoBehaviour
{
    [Header("[Cấu Hình Thành Phần UI]")]
    public Slider upgradeProgressBar;       
    public TMP_Text upgradeTimerText;       

    [Header("[VFX 3 Chức Năng Riêng Biệt]")]
    [Tooltip("Chức năng 1: Khói bụi mịn bám đất (Chỉ Active và phát 1 lần duy nhất lúc đặt nhà/di chuyển xuống)")]
    public ParticleSystem placementDustVFX; 
    
    [Tooltip("Chức năng 2: Cụm khói lớn + vụn vỡ (Chỉ Active và chạy lặp liên tục SUỐT thời gian đang xây hoặc nâng cấp)")]
    public ParticleSystem constructionLoopVFX;     
    
    [Tooltip("Chức năng 3: Ánh sáng aura quét dọc thân nhà (Chỉ Active và quét 1 lần duy nhất khi vừa hoàn thành)")]
    public ParticleSystem completionAuraVFX;      

    [Header("[Cấu Hình Thời Gian Xây Dựng]")]
    [SerializeField] private float buildDuration = 7f; 

    [Header("[HỆ THỐNG NGUỒN ÂM THANH ĐỘC LẬP (CÓ CỘNG TRỪ)]")]
    [Tooltip("Mảng AudioSource chuyên trị tiếng LOOP nâng cấp (Bấm dấu + để thêm nhiều nguồn phát song song)")]
    [SerializeField] private AudioSource[] upgradeAudioSources;
    
    [Tooltip("Mảng AudioSource chuyên trị tiếng ĐẶT NHÀ (Bấm dấu + để tách riêng, không đụng hàng với luồng nâng cấp)")]
    [SerializeField] private AudioSource[] placementAudioSources;

    [Header("[DANH SÁCH FILE ÂM THANH - PENTA DEV]")]
    [Tooltip("Danh sách file âm thanh nâng cấp lặp (Sẽ phát tương ứng theo thứ tự ô AudioSource ở trên)")]
    [SerializeField] private AudioClip[] upgradeLoopSFXPool;
    
    [Tooltip("Âm thanh đặt nhà xuống hoặc di chuyển xong đặt xuống (Chỉ dùng 1 file duy nhất)")]
    [SerializeField] private AudioClip placementSFX;

    private UpgradeableBuilding _ownerBuilding;
    private bool _isBuildingNew = false;

    private void Awake()
    {
        _ownerBuilding = GetComponentInParent<UpgradeableBuilding>();
        
        // Tự động tìm hạt con nếu chưa kéo thả ngoài Inspector để tránh NullReferenceException
        if (placementDustVFX == null) placementDustVFX = transform.Find("PlacementDustVFX")?.GetComponent<ParticleSystem>();
        if (constructionLoopVFX == null) constructionLoopVFX = transform.Find("ConstructionLoopVFX")?.GetComponent<ParticleSystem>();
        if (completionAuraVFX == null) completionAuraVFX = transform.Find("CompletionAuraVFX")?.GetComponent<ParticleSystem>();

        // Tự động bảo vệ: Tắt toàn bộ playOnAwake của hệ thống âm thanh nếu có
        InitAudioSources(upgradeAudioSources);
        InitAudioSources(placementAudioSources);

        HideProgress();
    }

    private void InitAudioSources(AudioSource[] sources)
    {
        if (sources == null) return;
        foreach (var src in sources)
        {
            if (src != null) src.playOnAwake = false;
        }
    }

    private void OnEnable()
    {
        if (_ownerBuilding != null)
        {
            BuildingProgressBridge.RegisterUI(_ownerBuilding, this);

            if (!_ownerBuilding.IsUpgrading && !_isBuildingNew)
            {
                HideProgress();
                DeactivateAllVFX();
                PlayPlacementVFX();
            }
        }
    }

    private void OnDisable()
    {
        if (_ownerBuilding != null)
        {
            BuildingProgressBridge.UnregisterUI(_ownerBuilding);
        }

        // Tắt toàn bộ tiếng loop nâng cấp nếu UI bị ẩn bất ngờ
        StopAllUpgradeLoopSFX();
    }

    private void DeactivateAllVFX()
    {
        if (placementDustVFX != null) placementDustVFX.gameObject.SetActive(false);
        if (constructionLoopVFX != null) constructionLoopVFX.gameObject.SetActive(false);
        if (completionAuraVFX != null) completionAuraVFX.gameObject.SetActive(false);
    }

    private void PlayPlacementVFX()
    {
        // 1. Dập sạch trạng thái nâng cấp lặp để tránh rác tiếng cũ
        StopAllUpgradeLoopSFX();

        if (placementDustVFX != null)
        {
            placementDustVFX.gameObject.SetActive(true);
            placementDustVFX.Stop();
            placementDustVFX.Play();
        }

        // 2. PHÁT TRÊN AUDIO SOURCE RIÊNG BIỆT: Chỉ luồng đặt nhà xử lý, độc lập 100%
        if (placementSFX != null && placementAudioSources != null && placementAudioSources.Length > 0)
        {
            foreach (var src in placementAudioSources)
            {
                if (src != null)
                {
                    src.PlayOneShot(placementSFX);
                }
            }
        }
    }

    public void UpdateProgress(float currentTimer, float totalDuration)
    {
        if (upgradeProgressBar != null)
        {
            if (!upgradeProgressBar.gameObject.activeSelf) upgradeProgressBar.gameObject.SetActive(true);
            upgradeProgressBar.maxValue = totalDuration;
            upgradeProgressBar.value = currentTimer;
        }

        if (upgradeTimerText != null)
        {
            if (!upgradeTimerText.gameObject.activeSelf) upgradeTimerText.gameObject.SetActive(true);
            float timeLeft = Mathf.Max(0f, totalDuration - currentTimer);
            upgradeTimerText.text = $"{timeLeft:F1}s";
        }

        if (constructionLoopVFX != null && !constructionLoopVFX.gameObject.activeSelf)
        {
            DeactivateAllVFX();
            constructionLoopVFX.gameObject.SetActive(true);
            constructionLoopVFX.Play();
        }

        // Luồng kích hoạt âm thanh nâng cấp lặp đồng thời dựa trên mảng
        if (_ownerBuilding != null && _ownerBuilding.IsUpgrading)
        {
            if (upgradeAudioSources != null && upgradeLoopSFXPool != null)
            {
                // Quét qua các cặp AudioSource và AudioClip tương ứng để phát song song
                int loopCount = Mathf.Min(upgradeAudioSources.Length, upgradeLoopSFXPool.Length);
                for (int i = 0; i < loopCount; i++)
                {
                    AudioSource src = upgradeAudioSources[i];
                    AudioClip clip = upgradeLoopSFXPool[i];

                    if (src != null && clip != null && !src.isPlaying)
                    {
                        src.clip = clip;
                        src.loop = true;
                        src.Play();
                    }
                }
            }
        }
    }

    public void HandleCompleteSequence()
    {
        StopAllUpgradeLoopSFX();

        if (constructionLoopVFX != null) constructionLoopVFX.gameObject.SetActive(false);

        if (completionAuraVFX != null)
        {
            completionAuraVFX.gameObject.SetActive(true);
            completionAuraVFX.Stop();
            completionAuraVFX.Play();
        }

        HideProgress();
        _isBuildingNew = false;
    }

    private void StopAllUpgradeLoopSFX()
    {
        if (upgradeAudioSources == null) return;
        foreach (var src in upgradeAudioSources)
        {
            if (src != null)
            {
                if (src.loop) src.Stop();
                src.loop = false;
                src.clip = null; 
            }
        }
    }

    public void HideProgress()
    {
        if (upgradeProgressBar != null) upgradeProgressBar.gameObject.SetActive(false);
        if (upgradeTimerText != null) upgradeTimerText.gameObject.SetActive(false);
    }
}

public static class BuildingProgressBridge
{
    private static System.Collections.Generic.Dictionary<UpgradeableBuilding, BuildingProgressBarUI> _uiRegistry = 
        new System.Collections.Generic.Dictionary<UpgradeableBuilding, BuildingProgressBarUI>();

    public static void RegisterUI(UpgradeableBuilding building, BuildingProgressBarUI ui)
    {
        if (!_uiRegistry.ContainsKey(building))
        {
            _uiRegistry.Add(building, ui);
        }
        else
        {
            _uiRegistry[building] = ui;
        }
    }

    public static void UnregisterUI(UpgradeableBuilding building)
    {
        if (_uiRegistry.ContainsKey(building))
        {
            _uiRegistry.Remove(building);
        }
    }

    public static BuildingProgressBarUI GetUI(UpgradeableBuilding building)
    {
        if (building != null && _uiRegistry.TryGetValue(building, out var ui)) return ui;
        return null;
    }
}

public static class UIManagerExtensions
{
    public static void UpdateUpgradeProgress(this UIManager uiManager, float currentTimer, float totalDuration)
    {
        var allBuildings = Object.FindObjectsByType<UpgradeableBuilding>(FindObjectsSortMode.None);
        foreach (var building in allBuildings)
        {
            if (building.IsUpgrading)
            {
                var targetUI = BuildingProgressBridge.GetUI(building);
                if (targetUI != null)
                {
                    targetUI.UpdateProgress(currentTimer, totalDuration);
                }
            }
        }
    }

    public static void HideUpgradeProgress(this UIManager uiManager)
    {
        var allUIs = Object.FindObjectsByType<BuildingProgressBarUI>(FindObjectsSortMode.None);
        foreach (var ui in allUIs)
        {
            if (ui.upgradeProgressBar != null && ui.upgradeProgressBar.gameObject.activeSelf)
            {
                ui.HandleCompleteSequence();
            }
            ui.HideProgress();
        }
    }
}