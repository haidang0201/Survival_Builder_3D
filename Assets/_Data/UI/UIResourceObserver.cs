using UnityEngine;

public class UIResourceObserver : MonoBehaviour
{
    public HUDController hud;

    void OnEnable()
    {
        var data = JsonDataManager.Ins;

        data.OnGoldChanged += OnGoldChanged;
        data.OnWoodChanged += OnWoodChanged;
        data.OnHPChanged += OnHPChanged;
    }

    void OnDisable()
    {
        var data = JsonDataManager.Ins;

        data.OnGoldChanged -= OnGoldChanged;
        data.OnWoodChanged -= OnWoodChanged;
        data.OnHPChanged -= OnHPChanged;
    }

    void Start()
    {
        var data = JsonDataManager.Ins;

        hud.UpdateGold(data.gold);
        hud.UpdateWood(data.wood);
        hud.UpdateHealth(data.hp);
    }

    void OnGoldChanged(int value)
    {
        hud.UpdateGold(value);
    }

    void OnWoodChanged(int value)
    {
        hud.UpdateWood(value);
    }

    void OnHPChanged(float value)
    {
        hud.UpdateHealth(value);
    }
}