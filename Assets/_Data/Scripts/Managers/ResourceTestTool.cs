using UnityEngine;

public class ResourceTestTool : MonoBehaviour
{
    [Header("Settings")]
    public int amount = 100; // Số lượng tăng/giảm mỗi lần bấm phím

    void Update()
    {
        if (JsonDataManager.Ins == null) return;

        // --- PHÍM TĂNG TÀI NGUYÊN ---
        if (Input.GetKeyDown(KeyCode.Alpha1)) // Phím 1: Tăng Vàng
        {
            JsonDataManager.Ins.AddGold(amount);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2)) // Phím 2: Tăng Gỗ
        {
            JsonDataManager.Ins.AddWood(amount);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3)) // Phím 3: Tăng Đá
        {
            JsonDataManager.Ins.AddStone(amount);
        }
        if (Input.GetKeyDown(KeyCode.Alpha0)) // Phím 0: Reset Tài Nguyên về 0
        {
            JsonDataManager.Ins.AddFood(amount); // Reset Thực Phẩm về 0
        }

        // --- PHÍM TRỪ TÀI NGUYÊN (Để test hiệu ứng số nhảy màu đỏ) ---
        if (Input.GetKeyDown(KeyCode.Alpha4)) // Phím 4: Trừ Vàng
        {
            JsonDataManager.Ins.AddGold(-amount);
        }
        if (Input.GetKeyDown(KeyCode.Alpha5)) // Phím 5: Trừ Gỗ
        {
            JsonDataManager.Ins.AddWood(-amount);
        }
        if (Input.GetKeyDown(KeyCode.Alpha6)) // Phím 6: Trừ Đá
        {
            JsonDataManager.Ins.AddStone(-amount);
        }
    }
}