/*
 * BuildingType.cs
 * Folder: Scripts/Building/
 * Người làm: DŨNG
 *
 * Enum định nghĩa toàn bộ loại công trình trong KHẨN HOANG
 * Dùng chung cho: BuildingData, BuildingCtrl, BuildingState, BuildingManager, GhostBuilding
 */

public enum BuildingType
{
    // ── NHÀ Ở ──────────────────────
    House,          // Nhà ở cơ bản của dân

    // ── SẢN XUẤT / THU THẬP ────────
    ForestHut,      // Lều rừng – worker đi chặt cây
    Sawmill,        // Xưởng cưa – chế biến gỗ

    // ── LƯU TRỮ ────────────────────
    Warehouse,      // Kho chứa tài nguyên

    // ── XÂY DỰNG ───────────────────
    HouseBuilder,   // Công trình đang xây dở
}