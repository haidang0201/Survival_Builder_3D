using UnityEngine;

/*
 * UIResourceObserver.cs
 * Đã cập nhật: Theo dõi sự thay đổi của Gỗ, Đá, Lúa (có sức chứa) và Vàng
 */
public class UIResourceObserver : MonoBehaviour
{
    public HUDController hud;
    private bool isSubscribed = false;

    void Start()
    {
        if (JsonDataManager.Ins == null)
        {
            Debug.LogError("JsonDataManager.Ins chưa được khởi tạo!");
            return;
        }

        SubscribeEvents();

        // Thêm câu lệnh if này để bảo vệ code:
        if (hud != null)
        {
            hud.UpdateGold(JsonDataManager.Ins.gold);
            hud.UpdateWood(JsonDataManager.Ins.wood);
            hud.UpdateStone(JsonDataManager.Ins.stone);
        }
        else
        {
            Debug.LogError("Bạn quên chưa kéo thả HUDController vào UIResourceObserver kìa!");
        }
    }

    void OnEnable()
    {
        // Nếu Object bị tắt đi bật lại sau khi Start đã chạy, ta đăng ký lại
        if (isSubscribed == false && JsonDataManager.Ins != null)
        {
            SubscribeEvents();
        }
    }

    void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        if (isSubscribed) return;
        
        var data = JsonDataManager.Ins;
        data.OnGoldChanged += OnGoldChanged;
        data.OnWoodChanged += OnWoodChanged;
        data.OnStoneChanged += OnStoneChanged;
        
        isSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscribed || JsonDataManager.Ins == null) return;

        var data = JsonDataManager.Ins;
        data.OnGoldChanged -= OnGoldChanged;
        data.OnWoodChanged -= OnWoodChanged;
        data.OnStoneChanged -= OnStoneChanged;
        
        isSubscribed = false;
    }
    void UpdateUIModeDay()
    {
        if (hud != null) hud.UpdateGold(value);
    }

    void UpdateUIModeNight()
    {
        if (hud != null) hud.UpdateWood(value);
    }

    void OnStoneChanged(int value)
    {
        if (hud != null) hud.UpdateStone(value);
    }
}