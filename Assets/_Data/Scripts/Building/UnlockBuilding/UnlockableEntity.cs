using UnityEngine;
using System;
using System.Collections.Generic;

/*
 * UnlockableEntity.cs
 * Folder: Scripts/Building/ or Scripts/World/
 * Dự án: KHẨN HOANG (PENTA DEV)
 * Người thực hiện: VŨ + DŨNG + ĐĂNG (Hoàn chỉnh luồng Tự Hủy Vùng Khóa 3D)
 * * NHIỆM VỤ: 
 * 1. Lưu trữ điều kiện tài nguyên và trạng thái Khóa/Mở của một khu vực hoặc công trình.
 * 2. Tách biệt Layer để không xung đột Raycast chọn nhà với UpgradeSelector.
 * 3. Tự động dọn dẹp (Destroy) Object vùng cản khi mở khóa thành công hoặc khi Load game.
 */

public class UnlockableEntity : MonoBehaviour
{
    [System.Serializable]
    public class UnlockRequirement
    {
        public int woodRequired;
        public int stoneRequired;
        public int foodRequired;
        public int goldRequired;
    }

    [Header("Cấu hình Khóa")]
    [Tooltip("ID duy nhất để Save/Load trạng thái vùng đất (Ví dụ: Zone_Bridge_01, Block_Forest_West)")]
    public string entityId;
    public string entityName = "Vùng Đất Bí Ẩn";
    [TextArea(2, 4)] public string entityDescription = "Nộp đủ tài nguyên để khai hoang và mở rộng khu vực này.";

    public UnlockRequirement requirement;

    [Header("Trạng thái Hiện tại")]
    [SerializeField] private bool isLocked = true;
    public bool IsLocked => isLocked;

    [Header("Liên kết Công năng (Tùy chọn)")]
    [Tooltip("Các script tính năng sẽ bị VÔ HIỆU HÓA khi khu vực bị khóa (Ví dụ: Mine, Generator)")]
    public MonoBehaviour[] targetScripts;

    [Tooltip("Các bức tường tàng hình vật lý chặn không cho dân làng đi qua khi chưa mở khóa")]
    public GameObject[] blockObstacles;

    private void Start()
    {
        // 1. Kiểm tra dữ liệu Save/Load từ các phiên chơi trước
        LoadUnlockState();

        // 2. Áp dụng trạng thái tương ứng
        ApplyState();
    }

    /// <summary>
    /// Điều chỉnh trạng thái bật/tắt của các tài nguyên, vật cản dựa trên biến isLocked
    /// </summary>
    public void ApplyState()
    {
        // Nếu đã mở khóa từ trước (do Load game), tiến hành tự hủy toàn bộ Object vùng khóa này
        if (!isLocked)
        {
            Debug.Log($"[UnlockSystem] Vùng đất '{entityName}' đã được mở từ trước. Tiến hành dọn dẹp Object.");
            Destroy(gameObject);
            return;
        }

        // Bật/Tắt các script công năng của công trình nằm bên trong vùng này
        foreach (var script in targetScripts)
        {
            if (script != null) script.enabled = !isLocked;
        }

        // Bật/Tắt các bức tường vật lý ngăn đường di chuyển trên Map
        foreach (var obstacle in blockObstacles)
        {
            if (obstacle != null) obstacle.SetActive(isLocked);
        }
    }

    /// <summary>
    /// Kiểm tra thời gian thực xem người chơi có đủ tài nguyên trong kho không
    /// </summary>
    public bool CanUnlock()
    {
        // Kiểm tra an toàn hệ thống dữ liệu lõi
        if (DialogNPC.Instance == null || JsonDataManager.Ins == null) return false;

        // Check tài nguyên Gỗ, Đá, Thịt thông qua ResourceManager
        bool hasEnoughResources = DialogNPC.Instance.CanAfford(requirement.woodRequired, requirement.foodRequired, requirement.stoneRequired);

        // Check tài nguyên Vàng thông qua JsonDataManager lõi
        bool hasEnoughGold = JsonDataManager.Ins.gold >= requirement.goldRequired;

        return hasEnoughResources && hasEnoughGold;
    }

    /// <summary>
    /// Hàm xử lý logic nộp tài nguyên và thực hiện mở khóa khu vực
    /// </summary>
    public bool ConfirmUnlock()
    {
        if (!isLocked) return false;
        if (!CanUnlock()) return false;

        // 1. Khấu trừ tài nguyên tiêu tốn từ kho lõi
        if (DialogNPC.Instance != null)
        {
            DialogNPC.Instance.Consume(requirement.woodRequired, requirement.foodRequired, requirement.stoneRequired);
        }
        if (JsonDataManager.Ins != null && requirement.goldRequired > 0)
        {
            JsonDataManager.Ins.AddGold(-requirement.goldRequired);
        }

        // 2. Thay đổi trạng thái logic của hệ thống sang Đã Mở
        isLocked = false;

        // 3. Lưu dữ liệu trạng thái xuống bộ nhớ máy ngay lập tức để không bị mất khi thoát game
        SaveUnlockState();

        Debug.Log($"[UnlockSystem] 🎉 Khai hoang mở rộng Map thành công: {entityName}");

        // 4. Tìm kiếm script va chạm Click đính kèm trên chính nó hoặc con của nó để thực hiện tự hủy
        UnlockWorldUI lockZoneScript = GetComponent<UnlockWorldUI>();
        if (lockZoneScript == null)
        {
            lockZoneScript = GetComponentInChildren<UnlockWorldUI>();
        }

        if (lockZoneScript != null)
        {
            // Ra lệnh xóa bỏ vùng Collider bọc quanh khu vực này
            lockZoneScript.DestroyLockZone();
        }
        else
        {
            // Phương án dự phòng: Nếu không tìm thấy script UnlockWorldUI, tự hủy trực tiếp chính nó
            ApplyState();
        }

        return true;
    }

    // ================= LOGIC ĐỒNG BỘ DỮ LIỆU SAVE / LOAD =================

    private void SaveUnlockState()
    {
        if (string.IsNullOrEmpty(entityId))
        {
            Debug.LogWarning($"[UnlockSystem] Cảnh báo: '{gameObject.name}' chưa được đặt Entity ID duy nhất! Không thể lưu dữ liệu.");
            return;
        }
        // Lưu trạng thái: 0 là Khóa, 1 là Đã Mở
        PlayerPrefs.SetInt("UnlockRegion_" + entityId, isLocked ? 0 : 1);
        PlayerPrefs.Save();
    }

    private void LoadUnlockState()
    {
        if (!string.IsNullOrEmpty(entityId) && PlayerPrefs.HasKey("UnlockRegion_" + entityId))
        {
            // Nếu giá trị lưu bằng 1 nghĩa là khu vực này đã được giải phóng
            isLocked = PlayerPrefs.GetInt("UnlockRegion_" + entityId) == 0;
        }
    }
}