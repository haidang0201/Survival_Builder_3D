using UnityEngine;

/*
 * UIResourceObserver.cs
 * Đã cập nhật: Theo dõi sự thay đổi của Gỗ, Đá, Lúa (có sức chứa) và Vàng
 */
public class UIResourceObserver : MonoBehaviour
{
    public HUDController hud;
    public Color dayTextColor = Color.black;
    public Color nightTextColor = Color.white;

    void OnEnable()
    {
        var data = JsonDataManager.Ins;

        // Đăng ký Event
        // data.OnGoldChanged += OnGoldChanged;
        data.OnWoodChanged += OnWoodChanged;
        data.OnStoneChanged += OnStoneChanged;
        data.OnFoodChanged += OnFoodChanged;
        data.OnHPChanged += OnHPChanged;
        DayNightManager.Ins.OnDayStart += UpdateUIModeDay;
        DayNightManager.Ins.OnNightStart += UpdateUIModeNight;
    }

    void OnDisable()
    {
        // Kiểm tra JsonDataManager.Ins để tránh lỗi NullReference khi thoát Game
        if (JsonDataManager.Ins == null) return;

        var data = JsonDataManager.Ins;

        // Hủy đăng ký Event
        //  data.OnGoldChanged -= OnGoldChanged;
        data.OnWoodChanged -= OnWoodChanged;
        data.OnStoneChanged -= OnStoneChanged;
        data.OnFoodChanged -= OnFoodChanged;
        data.OnHPChanged -= OnHPChanged;
        if (DayNightManager.Ins != null)
        {
            DayNightManager.Ins.OnDayStart -= UpdateUIModeDay;
            DayNightManager.Ins.OnNightStart -= UpdateUIModeNight;
        }
    }

    void Start()
    {
        var data = JsonDataManager.Ins;

        // Cập nhật giá trị ban đầu (với sức chứa hiện tại)
        // hud.UpdateGold(data.gold);
        hud.UpdateWood(data.wood, data.maxWood);
        hud.UpdateStone(data.stone, data.maxStone);
        hud.UpdateFood(data.food, data.maxFood);
        hud.UpdateHealth(data.hp);
    }
    void UpdateUIModeDay()
    {
        hud.SetTextColor(dayTextColor);
        Debug.Log("UI chuyển sang chế độ ban ngày");
    }

    void UpdateUIModeNight()
    {
        hud.SetTextColor(nightTextColor);
        Debug.Log("UI chuyển sang chế độ ban đêm");
    }

    // Các hàm lắng nghe sự kiện
    // void OnGoldChanged(int value) => hud.UpdateGold(value);

    void OnWoodChanged(int current, int max) => hud.UpdateWood(current, max);

    void OnStoneChanged(int current, int max) => hud.UpdateStone(current, max);

    void OnFoodChanged(int current, int max) => hud.UpdateFood(current, max);

    void OnHPChanged(float value) => hud.UpdateHealth(value);
}