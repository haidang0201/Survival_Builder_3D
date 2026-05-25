using UnityEngine;

/*
 * UIResourceObserver.cs
 * Folder: Scripts/UI/
 * Người làm: VŨ / DŨNG
 *
 * Cầu nối giữa JsonDataManager (event) và HUDController (hiển thị).
 * Observer pattern: subscribe event → nhận giá trị → đẩy lên HUD.
 *
 * Luồng:
 *   JsonDataManager.AddWood()
 *     → OnWoodChanged(current, max)
 *       → UIResourceObserver.OnWoodChanged(current, max)
 *         → HUDController.UpdateWood(current, max)
 *
 * Lưu ý: Subscribe trong OnEnable / Unsubscribe trong OnDisable
 * để tránh memory leak khi object bị tắt.
 */

public class UIResourceObserver : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // INSPECTOR
    // ──────────────────────────────────────────────

    [Tooltip("Kéo HUDController vào đây")]
    public HUDController hud;

    // ──────────────────────────────────────────────
    // PRIVATE
    // ──────────────────────────────────────────────

    private bool _isSubscribed;

    // ──────────────────────────────────────────────
    // LIFECYCLE
    // ──────────────────────────────────────────────

    private void Start()
    {
        if (hud == null)
        {
            Debug.LogError("[UIResourceObserver] HUDController chưa được gán trong Inspector!");
            return;
        }

        if (JsonDataManager.Ins == null)
        {
            Debug.LogError("[UIResourceObserver] JsonDataManager.Ins chưa tồn tại!");
            return;
        }

        Subscribe();

        // Push giá trị hiện tại lên HUD ngay lập tức (tránh HUD hiện 0 khi load game)
        RefreshHUD();
    }

    private void OnEnable()
    {
        // Đăng ký lại nếu object bị tắt rồi bật (sau khi Start đã chạy)
        if (!_isSubscribed && JsonDataManager.Ins != null)
            Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    // ──────────────────────────────────────────────
    // SUBSCRIBE / UNSUBSCRIBE
    // ──────────────────────────────────────────────

    private void Subscribe()
    {
        if (_isSubscribed) return;

        var dm = JsonDataManager.Ins;
        dm.OnGoldChanged += OnGoldChanged;
        dm.OnWoodChanged += OnWoodChanged;
        dm.OnStoneChanged += OnStoneChanged;
        dm.OnFoodChanged += OnFoodChanged;

        _isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_isSubscribed || JsonDataManager.Ins == null) return;

        var dm = JsonDataManager.Ins;
        dm.OnGoldChanged -= OnGoldChanged;
        dm.OnWoodChanged -= OnWoodChanged;
        dm.OnStoneChanged -= OnStoneChanged;
        dm.OnFoodChanged -= OnFoodChanged;

        _isSubscribed = false;
    }

    // ──────────────────────────────────────────────
    // EVENT HANDLERS  –  ký hiệu phải khớp JsonDataManager
    // ──────────────────────────────────────────────

    private void OnGoldChanged(int value)
    {
        hud?.UpdateGold(value);
    }

    private void OnWoodChanged(int current, int max)
    {
        hud?.UpdateWood(current, max);
    }

    private void OnStoneChanged(int current, int max)
    {
        hud?.UpdateStone(current, max);
    }

    private void OnFoodChanged(int current, int max)
    {
        hud?.UpdateFood(current, max);
    }

    // ──────────────────────────────────────────────
    // HELPERS
    // ──────────────────────────────────────────────

    /// <summary>
    /// Push toàn bộ giá trị hiện tại từ JsonDataManager lên HUD.
    /// Gọi khi vào game / load game để HUD không hiển thị 0.
    /// </summary>
    private void RefreshHUD()
    {
        if (hud == null || JsonDataManager.Ins == null) return;

        var dm = JsonDataManager.Ins;
        hud.UpdateGold(dm.gold);
        hud.UpdateWood(dm.wood, dm.maxWood);
        hud.UpdateStone(dm.stone, dm.maxStone);
        hud.UpdateFood(dm.food, dm.maxFood);
    }
}