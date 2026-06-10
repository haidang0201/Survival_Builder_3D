using UnityEngine;

/*
 * EndGameTestTrigger.cs
 * Dự án: KHẨN HOANG (PENTA DEV)
 * NHIỆM VỤ: Test EndGame bằng dữ liệu TỔNG TÍCH LŨY THỰC TẾ 
 * và ĐỒNG BỘ SỐ NGÀY THỰC TẾ lấy từ DayNightManager.
 */
public class EndGameTestTrigger : MonoBehaviour
{
    [Header("Cấu hình Phím Tắt Test")]
    [Tooltip("Phím bấm để kích hoạt EndGame tức thì khi chơi")]
    public KeyCode testKey = KeyCode.F;

    void Update()
    {
        // Khi bấm phím tắt để kiểm tra
        if (Input.GetKeyDown(testKey))
        {
            Debug.Log($"[TestTrigger] ⚠️ Đang quét dữ liệu THỰC TẾ (Ngày + Tài nguyên) bằng phím: {testKey}");
            TriggerEndGameWithRealData();
        }
    }

    private void TriggerEndGameWithRealData()
    {
        // 1. Kiểm tra an toàn hệ thống dữ liệu lõi
        if (JsonDataManager.Ins == null || EndGameUI.Instance == null)
        {
            Debug.LogError("[TestTrigger] ❌ Thiếu JsonDataManager hoặc EndGameUI trong Scene!");
            return;
        }

        // 2. LẤY SỐ NGÀY THỰC TẾ thời gian thực từ hệ thống quản lý ngày đêm của bạn
        int realDaysSurvived = 0;
        
        // Kiểm tra xem class DayNightManager của bạn đã chạy trong Scene chưa
        // Sử dụng class ẩn dưới tên DayNightManager (đang điều khiển đếm ngày của TimeUIController)
        if (DayNightManager.Ins != null)
        {
            realDaysSurvived = DayNightManager.Ins.CurrentDay;
        }
        else
        {
            Debug.LogWarning("[TestTrigger] ⚠️ Không tìm thấy DayNightManager.Ins trong Scene! Số ngày tạm tính là 0.");
        }

        // 3. LẤY TỔNG SỐ LƯỢNG tài nguyên tích lũy thật từ đầu trận đến giờ
        int finalTotalWood  = JsonDataManager.Ins.TotalWoodCollected;
        int finalTotalStone = JsonDataManager.Ins.TotalStoneCollected;
        int finalTotalFood  = JsonDataManager.Ins.TotalFoodCollected;
        int finalTotalGold  = JsonDataManager.Ins.TotalGoldCollected;

        // Lấy số lượng công trình đã xây dựng được từ hệ thống PlayerPrefs của bạn
        int finalTotalBuildings = PlayerPrefs.GetInt("Stat_Total_Buildings", 0);

        // 4. Lưu đồng bộ vào hệ thống PlayerPrefs để EndGameUI bóc tách dữ liệu
        PlayerPrefs.SetInt("Stat_Total_Wood", finalTotalWood);
        PlayerPrefs.SetInt("Stat_Total_Stone", finalTotalStone);
        PlayerPrefs.SetInt("Stat_Total_Food", finalTotalFood);
        PlayerPrefs.SetInt("Stat_Total_Gold", finalTotalGold);
        PlayerPrefs.SetInt("Stat_Total_Buildings", finalTotalBuildings);
        PlayerPrefs.Save();

        // 5. Kích hoạt hiển thị bảng tổng kết với SỐ NGÀY THỰC TẾ
        EndGameUI.Instance.TriggerEndGame(realDaysSurvived);

        Debug.Log($"[TestTrigger] ✅ TỔNG KẾT THÀNH CÔNG -> Ngày sinh tồn thật: {realDaysSurvived} Ngày | Gỗ tích lũy: {finalTotalWood}");
    }
}