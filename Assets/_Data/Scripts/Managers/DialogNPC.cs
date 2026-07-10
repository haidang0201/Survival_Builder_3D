using System;
using UnityEngine;

public class DialogNPC : MonoBehaviour
{
    public static DialogNPC Instance { get; private set; }

    // ĐÃ LOẠI BỎ: Các biến private wood, rice, stone cũ để tránh trùng lặp dữ liệu

    // C# Properties: Trỏ trực tiếp sang JsonDataManager làm gốc
    public int Wood
    {
        get => JsonDataManager.Ins != null ? JsonDataManager.Ins.wood : 0;
        set
        {
            if (JsonDataManager.Ins == null) return;
            int delta = value - JsonDataManager.Ins.wood;
            JsonDataManager.Ins.AddWood(delta);
        }
    }

    public int Stone
    {
        get => JsonDataManager.Ins != null ? JsonDataManager.Ins.stone : 0;
        set
        {
            if (JsonDataManager.Ins == null) return;
            int delta = value - JsonDataManager.Ins.stone;
            JsonDataManager.Ins.AddStone(delta);
        }
    }

    public int Rice // Map thuộc tính Rice của bạn với thuộc tính Food của JsonDataManager
    {
        get => JsonDataManager.Ins != null ? JsonDataManager.Ins.food : 0;
        set
        {
            if (JsonDataManager.Ins == null) return;
            int delta = value - JsonDataManager.Ins.food;
            JsonDataManager.Ins.AddFood(delta);
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Kiểm tra xem người chơi có đủ tài nguyên để xây/nâng cấp không (Đọc từ JsonDataManager)
    /// </summary>
    public bool CanAfford(int woodCost, int foodCost, int stoneCost)
    {
        if (JsonDataManager.Ins == null) return false;
        return JsonDataManager.Ins.wood >= woodCost &&
               JsonDataManager.Ins.food >= foodCost &&
               JsonDataManager.Ins.stone >= stoneCost;
    }

    /// <summary>
    /// Trừ tài nguyên trực tiếp vào JsonDataManager bằng giá trị âm
    /// </summary>
    public bool Consume(int woodCost, int foodCost, int stoneCost)
    {
        if (!CanAfford(woodCost, foodCost, stoneCost))
        {
            Debug.LogWarning($"[RESOURCE_SYSTEM] Thất bại! Thiếu tài nguyên. Cần: Gỗ({woodCost}), Lúa({foodCost}), Đá({stoneCost})");
            return false;
        }

        // Tác động thẳng vào dữ liệu lõi thông qua hàm Add
        JsonDataManager.Ins.AddWood(-woodCost);
        JsonDataManager.Ins.AddFood(-foodCost);
        JsonDataManager.Ins.AddStone(-stoneCost);
        // Trước khi return true, hãy thêm dòng này:
        if (JsonDataManager.Ins != null)
        {
            JsonDataManager.Ins.BroadcastAllResources();
        }

        Debug.Log($"[RESOURCE_SYSTEM] Tiêu thụ thành công! Kho còn: Gỗ({Wood}), Lúa({Rice}), Đá({Stone})");
        return true;
    }

    [ContextMenu("Debug/Add 500 All")]
    public void AddDebugResources()
    {
        if (JsonDataManager.Ins == null) return;
        JsonDataManager.Ins.AddWood(500);
        JsonDataManager.Ins.AddFood(500);
        JsonDataManager.Ins.AddStone(500);
        Debug.Log("[RESOURCE_SYSTEM] Đã nạp thêm 500 vào JsonDataManager để test!");
    }
}