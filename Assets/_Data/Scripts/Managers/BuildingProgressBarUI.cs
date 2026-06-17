using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
 * BuildingProgressBarUI.cs
 * Folder: Scripts/UI/
 * Dự án: KHẨN HOANG (PENTA DEV)
 * Người thực hiện chỉnh sửa: VŨ (Luồng UI)
 * * GIẢI QUYẾT TRIỆT ĐỂ: Tự động phát hiện và dập tắt thanh tiến độ bám đuôi 
 * khi nhà thật được SetActive(true) trở lại từ luồng di chuyển (Move Mode).
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

    private UpgradeableBuilding _ownerBuilding;
    private bool _isBuildingNew = false;

    private void Awake()
    {
        _ownerBuilding = GetComponentInParent<UpgradeableBuilding>();
        
        // Tự động tìm hạt con nếu chưa kéo thả ngoài Inspector để tránh NullReferenceException
        if (placementDustVFX == null) placementDustVFX = transform.Find("PlacementDustVFX")?.GetComponent<ParticleSystem>();
        if (constructionLoopVFX == null) constructionLoopVFX = transform.Find("ConstructionLoopVFX")?.GetComponent<ParticleSystem>();
        if (completionAuraVFX == null) completionAuraVFX = transform.Find("CompletionAuraVFX")?.GetComponent<ParticleSystem>();

        HideProgress();
    }

    private void OnEnable()
    {
        // Đăng ký cổng nhận tín hiệu riêng cho nhà này với Bridge cứu cánh
        if (_ownerBuilding != null)
        {
            BuildingProgressBridge.RegisterUI(_ownerBuilding, this);

            // =================================================================================
            // CHÌA KHÓA VÀNG: Kiểm tra xem nhà có đang thực sự nâng cấp không khi được bật Active lại.
            // Nếu căn nhà KHÔNG trong trạng thái nâng cấp (tức là vừa di chuyển đặt xuống đất), 
            // lập tức ép ẩn thanh tiến độ và dập tắt toàn bộ hạt bám đuôi bậy bạ bạ.
            // =================================================================================
            if (!_ownerBuilding.IsUpgrading && !_isBuildingNew)
            {
                HideProgress();
                DeactivateAllVFX();
                
                // MẸO: Nếu muốn khi vừa đặt nhà di chuyển xuống đất có phụt một chút khói bụi đất (PlacementDust) cho sinh động:
                PlayPlacementVFX();
            }
            // =================================================================================
        }
    }

    private void OnDisable()
    {
        // Hủy đăng ký khi bị tắt để tránh rác bộ nhớ
        if (_ownerBuilding != null)
        {
            BuildingProgressBridge.UnregisterUI(_ownerBuilding);
        }
    }

    private void DeactivateAllVFX()
    {
        if (placementDustVFX != null) placementDustVFX.gameObject.SetActive(false);
        if (constructionLoopVFX != null) constructionLoopVFX.gameObject.SetActive(false);
        if (completionAuraVFX != null) completionAuraVFX.gameObject.SetActive(false);
    }

    private void PlayPlacementVFX()
    {
        if (placementDustVFX != null)
        {
            placementDustVFX.gameObject.SetActive(true);
            placementDustVFX.Stop();
            placementDustVFX.Play();
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

        // Bật khói thi công lặp liên tục khi đang nâng cấp thật sự
        if (constructionLoopVFX != null && !constructionLoopVFX.gameObject.activeSelf)
        {
            DeactivateAllVFX();
            constructionLoopVFX.gameObject.SetActive(true);
            constructionLoopVFX.Play();
        }
    }

    public void HandleCompleteSequence()
    {
        // Tắt khói thi công liên tục đi
        if (constructionLoopVFX != null) constructionLoopVFX.gameObject.SetActive(false);

        // Kích hoạt Active khói Aura quét 1 lần duy nhất báo hiệu hoàn thành xong xuôi
        if (completionAuraVFX != null)
        {
            completionAuraVFX.gameObject.SetActive(true);
            completionAuraVFX.Stop();
            completionAuraVFX.Play();
        }

        HideProgress();
        _isBuildingNew = false;
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