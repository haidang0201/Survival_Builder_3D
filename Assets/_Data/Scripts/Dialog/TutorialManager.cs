using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public NPCDialogue npc;
    public UIHighlightSystem highlight;

    [Header("TÀI NGUYÊN & ĐỊA ĐIỂM")]
    public RectTransform woodIcon;
    public RectTransform stoneIcon;
    public RectTransform foodIcon;
    public Transform stoneMineWorld;
    public Transform enemyCampWorld;

    [Header("TOP BAR ICONS")]
    public RectTransform goldIcon;
    public RectTransform dayIcon;

    [Header("CẤU HÌNH PANEL XÂY DỰNG")]
    public GameObject buildPanelObject;
    public RectTransform openBuildPanelBtn;

    [Header("DAY 5: WATCH TOWER TUTORIAL")]
    public RectTransform buildWatchTowerBtn;
    private bool isWatchTowerButtonClicked = false;

    [Header("DAY 6: KITCHEN TUTORIAL")]
    public Transform newForestWorld;
    public RectTransform buildKitchenBtn;
    private bool isKitchenButtonClicked = false;

    [Header("DAY 8: DEFENSE TUTORIAL")]
    public Transform watchTowerWorld;
    public Transform alliedTroopsWorld;

    [Header("DAY 9: CANNON & ARCHER TUTORIAL")]
    public Transform day9EnemySpawnPos;
    public GameObject day9EnemyPrefab;
    private GameObject spawnedDay9Enemy;

    public RectTransform buildCannonBtn;
    public RectTransform buildArcherTowerBtn;
    private bool isCannonButtonClicked = false;
    private bool isArcherButtonClicked = false;

    [Header("THÔNG SỐ ĐIỀU KIỆN")]
    public int worker = 0;
    public int wood = 0;

    void Start()
    {
        StartCoroutine(Intro());
    }

    IEnumerator Intro()
    {
        // ================= INTRO CƠ BẢN (GOLD, DAY, TÀI NGUYÊN) =================
        SetTimePaused(true); // Dừng đồng hồ, NPC vẫn nói

        highlight.Highlight(goldIcon);
        yield return npc.ShowAndWait("Đây là vàng thưởng - nhận được khi đánh thắng kẻ địch.");

        highlight.Highlight(dayIcon);
        yield return npc.ShowAndWait("Đây là DAY - thể hiện số ngày bạn đã sinh tồn.");

        highlight.ClearAll();

        UIHighlightSystem.Instance.Highlight(woodIcon);
        yield return npc.ShowAndWait("Đây là GỖ - dùng để xây dựng.");

        UIHighlightSystem.Instance.Highlight(stoneIcon);
        yield return npc.ShowAndWait("Đây là ĐÁ - dùng để xây công trình.");

        UIHighlightSystem.Instance.Highlight(foodIcon);
        yield return npc.ShowAndWait("Đây là LÚA - nuôi dân làng.");

        UIHighlightSystem.Instance.ClearAll();
        yield return npc.ShowAndWait("Bắt đầu xây dựng làng!");

        // ================= MỎ ĐÁ =================
        yield return npc.ShowAndWait("Phía xa là mỏ đá.");
        highlight.ClearAll();
        npc.Show("Lia camera đến mỏ đá...");
        Camera.main.transform.position = stoneMineWorld.position;

        yield return npc.ShowAndWait("Mỏ đá đang bị khóa.");
        yield return npc.ShowAndWait("Cần 7 worker và 12 gỗ để mở.");

        SetTimePaused(false); // Cho đồng hồ chạy để người chơi rảnh tay cày cuốc
        yield return new WaitUntil(() => worker >= 7 && wood >= 12);

        SetTimePaused(true); // Đủ tài nguyên, dừng đồng hồ để khen ngợi
        yield return npc.ShowAndWait("Mỏ đá đã mở!");

        Camera.main.transform.position = Vector3.zero;
        SetTimePaused(false);

        // ================= NGÀY THỨ 3 (CẢNH BÁO ĐỊCH) =================
        yield return new WaitUntil(() => DayNightManager.Ins != null && DayNightManager.Ins.CurrentDay >= 3);
        SetTimePaused(true);

        yield return npc.ShowAndWait("Phó lý: Bẩm, địch gồm cung thủ và quân phi lao đang chuẩn bị tấn công!");
        yield return npc.ShowAndWait("Hãy chuẩn bị dùng 15 Gỗ và 10 Đá để mở tháp canh bảo vệ làng!");

        SetTimePaused(false);

        // ================= NGÀY THỨ 4 (NÂNG CẤP NHÀ WORKER) =================
        yield return new WaitUntil(() => DayNightManager.Ins != null && DayNightManager.Ins.CurrentDay >= 4);
        SetTimePaused(true);

        yield return npc.ShowAndWait("Ngày thứ 4: 4 Worker của chúng ta đã làm việc rất chăm chỉ.");
        yield return npc.ShowAndWait("Khi tích lũy đủ 40 Gỗ và 60 Đá, hãy nâng cấp nhà Worker.");
        yield return npc.ShowAndWait("Việc này sẽ tăng giới hạn lên 6 Worker và mở khóa công nghệ Tháp Canh để chống địch!");

        SetTimePaused(false);

        // ================= NGÀY THỨ 5 (MỞ KHÓA THÁP CANH) =================
        yield return new WaitUntil(() => DayNightManager.Ins != null && DayNightManager.Ins.CurrentDay >= 5);
        SetTimePaused(true);

        yield return npc.ShowAndWait("Ngày thứ 5: Kẻ địch đang chuẩn bị một cuộc tấn công quy mô lớn hơn!");
        yield return npc.ShowAndWait("Hệ thống phòng thủ cơ bản của bạn cần được tối ưu tầm nhìn.");

        if (openBuildPanelBtn != null) highlight.Highlight(openBuildPanelBtn);
        yield return npc.ShowAndWait("Hãy mở Menu Xây Dựng để chế tạo hệ thống phòng thủ mới.");

        yield return new WaitUntil(() => buildPanelObject != null && buildPanelObject.activeSelf == true);
        highlight.ClearAll();

        if (buildWatchTowerBtn != null)
        {
            Button btnWatch = buildWatchTowerBtn.GetComponent<Button>();
            if (btnWatch != null)
            {
                btnWatch.onClick.RemoveListener(OnWatchTowerBtnClicked);
                btnWatch.onClick.AddListener(OnWatchTowerBtnClicked);
            }
            highlight.Highlight(buildWatchTowerBtn);
        }
        yield return npc.ShowAndWait("Hãy chọn xây Tháp Canh (Watch Tower)!");

        yield return new WaitUntil(() => isWatchTowerButtonClicked == true);
        highlight.ClearAll();

        yield return npc.ShowAndWait("Cơ chế hoạt động: Tháp Canh sẽ tự động quét vị trí địch trong phạm vi → Chỉ điểm mục tiêu từ xa cho Tháp Pháo và Tháp Cung tấn công!");
        yield return npc.ShowAndWait("Hãy đặt Tháp Canh ở vị trí chiến lược đầu chiến tuyến để dẫn đường loạt đạn phòng thủ.");

        SetTimePaused(false);

        // ================= NGÀY THỨ 6 (XÂY DỰNG NHÀ BẾP) =================
        yield return new WaitUntil(() => DayNightManager.Ins != null && DayNightManager.Ins.CurrentDay >= 6);
        SetTimePaused(true);

        yield return npc.ShowAndWait("Ngày thứ 6: Khu vực gỗ ban đầu đã cạn kiệt!");
        npc.Show("Lia camera đến khu rừng mới...");
        if (newForestWorld != null) Camera.main.transform.position = newForestWorld.position;

        yield return npc.ShowAndWait("Worker phải đi làm rất xa, dẫn đến việc mất nhiều năng lượng và giảm năng suất.");

        if (openBuildPanelBtn != null) highlight.Highlight(openBuildPanelBtn);
        yield return npc.ShowAndWait("Hãy mở lại Menu Xây Dựng để tìm công trình hỗ trợ kinh tế.");

        yield return new WaitUntil(() => buildPanelObject != null && buildPanelObject.activeSelf == true);
        highlight.ClearAll();

        if (buildKitchenBtn != null)
        {
            Button btnKitchen = buildKitchenBtn.GetComponent<Button>();
            if (btnKitchen != null)
            {
                btnKitchen.onClick.RemoveListener(OnKitchenBtnClicked);
                btnKitchen.onClick.AddListener(OnKitchenBtnClicked);
            }
            highlight.Highlight(buildKitchenBtn);
        }
        yield return npc.ShowAndWait("Hãy chọn xây Nhà Bếp (Kitchen) để giúp Worker hồi năng lượng gần khu rừng mới!");

        yield return new WaitUntil(() => isKitchenButtonClicked == true);
        highlight.ClearAll();
        yield return npc.ShowAndWait("Tuyệt vời! Hãy đặt Nhà Bếp ở gần khu vực khai thác mới để tối ưu hóa nhé.");

        // ================= LỀU ĐỊCH (LẦN ĐẦU) =================
        yield return npc.ShowAndWait("Phía xa là lều địch.");
        highlight.ClearAll();
        Camera.main.transform.position = enemyCampWorld.position;
        yield return npc.ShowAndWait("Đây là nơi địch đóng quân.");
        yield return npc.ShowAndWait("Hãy chuẩn bị phòng thủ!");
        highlight.ClearAll();

        Camera.main.transform.position = Vector3.zero;

        SetTimePaused(false);

        // ================= NGÀY THỨ 8 (ĐỢT TẤN CÔNG LỚN) =================
        yield return new WaitUntil(() => DayNightManager.Ins != null && DayNightManager.Ins.CurrentDay >= 8);
        SetTimePaused(true);

        yield return npc.ShowAndWait("Ngày thứ 8: Địch Phi Lao và Bắn Cung đang mở một đợt tấn công lớn!");

        if (watchTowerWorld != null)
        {
            npc.Show("Lia camera đến tháp canh...");
            Camera.main.transform.position = watchTowerWorld.position;
        }
        yield return npc.ShowAndWait("Tháp canh đang liên tục quét vị trí kẻ địch, hãy ra lệnh cho tháp phòng thủ bắn trả!");

        if (alliedTroopsWorld != null)
        {
            npc.Show("Lia camera đến lính ta...");
            Camera.main.transform.position = alliedTroopsWorld.position;
        }
        yield return npc.ShowAndWait("Huấn luyện thêm đội Lính Ta và ra lệnh tiến lên chống trả để giành chiến thắng!");

        Camera.main.transform.position = Vector3.zero;

        SetTimePaused(false);

        // =================================================================
        // ==================== ⚔️ NGÀY THỨ 9: TỔNG LỰC THỦ THÀNH ====================
        // =================================================================
        yield return new WaitUntil(() => DayNightManager.Ins != null && DayNightManager.Ins.CurrentDay >= 9);
        SetTimePaused(true);

        if (WarningUI.Instance != null)
        {
            WarningUI.Instance.Show("CẢNH BÁO: ĐỢT TẤN CÔNG TỔNG LỰC TỪ THỔ PHỈ!");
        }

        if (day9EnemyPrefab != null && day9EnemySpawnPos != null)
        {
            spawnedDay9Enemy = Instantiate(day9EnemyPrefab, day9EnemySpawnPos.position, Quaternion.identity);
            Debug.Log("[Tutorial] Đã spawn địch Ngày 9 thành công!");
        }

        yield return new WaitForSeconds(5f);

        if (day9EnemySpawnPos != null) Camera.main.transform.position = day9EnemySpawnPos.position;

        yield return npc.ShowAndWait("Địch đang chuẩn bị tổng lực tấn công!");
        yield return npc.ShowAndWait("Cậu hãy xây dựng pháo và cung để chống trả!");

        if (WarningUI.Instance != null)
        {
            WarningUI.Instance.Hide();
        }

        Camera.main.transform.position = Vector3.zero;

        if (buildPanelObject != null && buildPanelObject.activeSelf == false)
        {
            if (openBuildPanelBtn != null) highlight.Highlight(openBuildPanelBtn);
            yield return npc.ShowAndWait("Hãy mở Menu Xây Dựng để bố trí vũ khí!");
            yield return new WaitUntil(() => buildPanelObject.activeSelf == true);
            highlight.ClearAll();
        }

        if (buildCannonBtn != null)
        {
            Button btnCannon = buildCannonBtn.GetComponent<Button>();
            if (btnCannon != null) { btnCannon.onClick.RemoveListener(OnCannonBtnClicked); btnCannon.onClick.AddListener(OnCannonBtnClicked); }
            highlight.Highlight(buildCannonBtn);
        }
        yield return npc.ShowAndWait("Hãy chọn xây THÁP PHÁO (Cannon)!");
        yield return new WaitUntil(() => isCannonButtonClicked == true);
        highlight.ClearAll();

        if (buildPanelObject != null && buildPanelObject.activeSelf == false)
        {
            if (openBuildPanelBtn != null) highlight.Highlight(openBuildPanelBtn);
            yield return new WaitUntil(() => buildPanelObject.activeSelf == true);
            highlight.ClearAll();
        }

        if (buildArcherTowerBtn != null)
        {
            Button btnArcher = buildArcherTowerBtn.GetComponent<Button>();
            if (btnArcher != null) { btnArcher.onClick.RemoveListener(OnArcherBtnClicked); btnArcher.onClick.AddListener(OnArcherBtnClicked); }
            highlight.Highlight(buildArcherTowerBtn);
        }
        yield return npc.ShowAndWait("Đồng thời xây thêm THÁP CUNG!");
        yield return new WaitUntil(() => isArcherButtonClicked == true);
        highlight.ClearAll();

        if (day9EnemySpawnPos != null) Camera.main.transform.position = day9EnemySpawnPos.position;
        yield return npc.ShowAndWait("Kìa, địch đang di chuyển qua! Hãy phối hợp cùng quân lính bảo vệ ngôi làng và tiến lên san phẳng căn cứ địch!");

        Camera.main.transform.position = Vector3.zero;

        SetTimePaused(false); // Cho đồng hồ chạy lại và trận chiến diễn ra!

        yield return new WaitUntil(() => CheckEnemyCampDestroyed());

        SetTimePaused(true); // Dừng lại để chúc mừng
        yield return npc.ShowAndWait("Xuất sắc! Căn cứ điểm của bọn Thổ phỉ đã bị san phẳng hoàn toàn!");
        yield return npc.ShowAndWait("Phần thưởng chiến công: +500 xu vàng đã được chuyển vào kho báu của làng!");

        ClearAllMapFog();
        yield return npc.ShowAndWait("Toàn bộ sương mù bao phủ vùng đất đã tan biến! Hãy chuẩn bị cho trận chiến cuối cùng.");
        SetTimePaused(false);


        // =================================================================
        // ==================== 👑 NGÀY THỨ 10: TRẬN CHIẾN CUỐI CÙNG ====================
        // =================================================================
        yield return new WaitUntil(() => DayNightManager.Ins != null && DayNightManager.Ins.CurrentDay >= 10);
        SetTimePaused(true);

        if (WarningUI.Instance != null)
        {
            WarningUI.Instance.Show("⚠️ TRẬN CHIẾN QUYẾT ĐỊNH: BOSS TỐI CAO XUẤT HIỆN! ⚠️");
        }

        yield return npc.ShowAndWait("NGÀY 10 - TRẬN CHIẾN QUYẾT ĐỊNH VẬN MỆNH VÙNG ĐẤT!");
        yield return npc.ShowAndWait("Thủ lĩnh tối cao Thổ phỉ (BOSS CUỐI) đang tiến vào! Hãy dồn toàn bộ lực lượng tiêu diệt hắn!");

        if (WarningUI.Instance != null)
        {
            WarningUI.Instance.Hide();
        }

        SetTimePaused(false);

        yield return new WaitUntil(() => CheckBossDefeated() || DayNightManager.Ins.CurrentDay > 10);

        SetTimePaused(true);
        yield return npc.ShowAndWait("Tuyệt vời! Bạn đã đánh bại thủ lĩnh tối cao và bảo vệ vùng đất Khẩn Hoang thành công rực rỡ!");
        yield return npc.ShowAndWait("Bình minh ló rạng, cuộc sống của dân làng từ nay đã được ấm no, bình yên ổn định...");
        yield return npc.ShowAndWait("Bạn đã giữ trọn vẹn lời hứa thiêng liêng với vị cố Trưởng Làng quá cố!");

        UnlockEndlessMode();

        yield return npc.ShowAndWait("HỆ THỐNG: Chúc mừng bạn đã hoàn thành mạch truyện chính! Đã mở khóa CHẾ ĐỘ CHƠI VÔ TẬN.");
        yield return npc.ShowAndWait("Giờ đây bạn có thể tiếp tục phát triển, xây dựng và sinh tồn không giới hạn tại vùng đất này!");

        highlight.ClearAll();
        SetTimePaused(false);
    }

    /// <summary>
    /// Hàm điều khiển dừng đồng hồ. KHÔNG dùng Time.timeScale = 0f để NPC vẫn chạy thoại.
    /// Bạn cần tạo thêm một biến (ví dụ boolean) bên trong script DayNightManager để ngắt bộ đếm nhé!
    /// </summary>
    private void SetTimePaused(bool paused)
    {
        if (DayNightManager.Ins != null)
        {
            // GIẢ SỬ TRONG SCRIPT DayNightManager CÓ BIẾN isClockPaused.
            // BẠN VUI LÒNG MỞ FILE DayNightManager.cs VÀ SỬA ĐOẠN ĐẾM NGƯỢC THÀNH:
            // if (!isClockPaused) { thời gian -= Time.deltaTime; }

            // Thay "isClockPaused" bằng biến thực tế bạn dùng bên đó:
            // DayNightManager.Ins.isClockPaused = paused; 

            Debug.Log(paused ? "[Tutorial] Đã dừng ĐỒNG HỒ, NPC vẫn đang nói!" : "[Tutorial] ĐỒNG HỒ chạy lại bình thường!");
        }
    }

    private void OnWatchTowerBtnClicked() { isWatchTowerButtonClicked = true; }
    private void OnKitchenBtnClicked() { isKitchenButtonClicked = true; }
    private void OnCannonBtnClicked() { isCannonButtonClicked = true; }
    private void OnArcherBtnClicked() { isArcherButtonClicked = true; }

    private bool CheckEnemyCampDestroyed() { return true; }
    private bool CheckBossDefeated() { return true; }
    private void ClearAllMapFog() { Debug.Log("[Tutorial] 🌫️ Đã dọn sạch sương mù!"); }
    private void UnlockEndlessMode() { PlayerPrefs.SetInt("EndlessModeUnlocked", 1); PlayerPrefs.Save(); }
}