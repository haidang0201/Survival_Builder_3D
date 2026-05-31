using System;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    // Singleton Instance để mọi script khác gọi tới dễ dàng
    public static ResourceManager Instance { get; set; }

    [Header("Kho Tài Nguyên Hiện Có")]
    [SerializeField] private int wood = 100;
    [SerializeField] private int rice = 100;
    [SerializeField] private int stone = 100;

    // Sự kiện (Event) thông báo khi có bất kỳ tài nguyên nào thay đổi
    // UI chỉ cần "lắng nghe" event này để tự động cập nhật số liệu
    public static event Action OnResourcesChanged;

    // C# Properties: Đóng gói dữ liệu an toàn, tự động kích hoạt Event và chặn số âm
    public int Wood
    {
        get => wood;
        set
        {
            wood = Mathf.Max(0, value); // Đảm bảo tài nguyên không bao giờ bị < 0
            OnResourcesChanged?.Invoke();
        }
    }

    public int Rice
    {
        get => rice;
        set
        {
            rice = Mathf.Max(0, value);
            OnResourcesChanged?.Invoke();
        }
    }

    public int Stone
    {
        get => stone;
        set
        {
            stone = Mathf.Max(0, value);
            OnResourcesChanged?.Invoke();
        }
    }

    private void Awake()
    {
        // Khởi tạo Singleton an toàn
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Giữ kho tài nguyên xuyên suốt các Scene
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Kiểm tra xem người chơi có đủ tài nguyên để xây/nâng cấp không
    /// </summary>
    public bool CanAfford(int woodCost, int riceCost, int stoneCost)
    {
        return wood >= woodCost && rice >= riceCost && stone >= stoneCost;
    }

    /// <summary>
    /// Trừ tài nguyên khi dùng để xây dựng hoặc nâng cấp công trình
    /// </summary>
    public bool Consume(int woodCost, int riceCost, int stoneCost)
    {
        if (!CanAfford(woodCost, riceCost, stoneCost))
        {
            Debug.LogWarning($"[RESOURCE_SYSTEM] Thất bại! Thiếu tài nguyên. Cần: Gỗ({woodCost}), Lúa({riceCost}), Đá({stoneCost})");
            return false;
        }

        // Trừ trực tiếp vào các Properties để kích hoạt Event cập nhật UI tự động
        Wood -= woodCost;
        Rice -= riceCost;
        Stone -= stoneCost;

        Debug.Log($"[RESOURCE_SYSTEM] Tiêu thụ thành công! Kho còn: Gỗ({wood}), Lúa({rice}), Đá({stone})");
        return true;
    }

    // Tính năng phụ: Tạo nút bấm nạp nhanh tài nguyên ngay trong Unity Editor để bạn tiện test game
    [ContextMenu("Debug/Add 500 All")]
    public void AddDebugResources()
    {
        Wood += 500;
        Rice += 500;
        Stone += 500;
        Debug.Log("[RESOURCE_SYSTEM] Đã hack thêm 500 mỗi loại để test!");
    }
}