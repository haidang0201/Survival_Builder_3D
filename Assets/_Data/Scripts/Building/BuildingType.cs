/*
 * BuildingType.cs
 * Folder: Scripts/Systems/Json/
 * Người làm: DŨNG
 *
 * Enum định nghĩa toàn bộ loại công trình trong game KHẨN HOANG
 * Dùng chung cho BuildingData, BuildingState, TestSaveLoad
 */

public enum BuildingType
{
    // ── NHÀ Ở ──────────────────────────────
    House,              // Nhà ở cơ bản của dân

    // ── SẢN XUẤT / THU THẬP ────────────────
    ForestHut,          // Lều rừng – worker đi chặt cây
    Sawmill,            // Xưởng cưa – chế biến gỗ

    // ── LƯU TRỮ ────────────────────────────
    Warehouse,          // Kho chứa tài nguyên

    // ── XÂY DỰNG ───────────────────────────
    HouseBuilder,       // Công trình đang xây (builder làm việc)

    // ── MÔI TRƯỜNG (không phải building hẳn nhưng cần track) ──
    Tree,               // Cây trong map (chặt được)
}