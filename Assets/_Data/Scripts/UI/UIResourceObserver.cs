using UnityEngine;

/*
 * UIResourceObserver.cs
 * Folder: Scripts/UI/
 * Người làm: VŨ / DŨNG
 *
 * Cầu nối giữa JsonDataManager (event) và HUDController (hiển thị).
 */

public class UIResourceObserver : MonoBehaviour
{
    [Tooltip("Kéo HUDController vào đây")]
    public HUDController hud;

    private bool _isSubscribed;

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
        RefreshHUD();
    }

    private void OnEnable()
    {
        if (!_isSubscribed && JsonDataManager.Ins != null)
            Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

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

    private void OnGoldChanged(int value) => hud?.UpdateGold(value);
    private void OnWoodChanged(int value) => hud?.UpdateWood(value);
    private void OnStoneChanged(int value) => hud?.UpdateStone(value);
    private void OnFoodChanged(int value) => hud?.UpdateFood(value);

    private void RefreshHUD()
    {
        if (hud == null || JsonDataManager.Ins == null) return;

        var dm = JsonDataManager.Ins;
        hud.UpdateGold(dm.gold);
        hud.UpdateWood(dm.wood);
        hud.UpdateStone(dm.stone);
        hud.UpdateFood(dm.food);
    }
}