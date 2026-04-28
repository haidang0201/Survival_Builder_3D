using UnityEngine;

public class LoadBehavior : Singleton<LoadBehavior>
{
    // // 1. Khai báo các "ổ cắm" để các thành viên khác kéo vào
    // [Header("Core Systems")]
    // public JsonDataManager Data;     // Của Dũng
    // public HUDController UI;         // Của Dũng/Vũ
    // public GameplayManager Gameplay; // Của Tiến
    // public SoundManager Sound;

    // protected override void Awake()
    // {
    //     // 2. Sử dụng hàm của bạn: truyền false để giữ lại qua các Scene
    //     MakeSingleton(false);

    //     // 3. Tự động tìm tham chiếu nếu Leader quên kéo tay trong Inspector
    //     if (Data == null) Data = FindObjectOfType<JsonDataManager>();
    //     if (UI == null) UI = FindObjectOfType<HUDController>();
    //     // ... tương tự cho các manager khác
    // }
}