using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [Header("TYPING SOUND")]
    [Tooltip("AudioSource có clip 'tick' ngắn — âm thanh chạy theo từng chữ trong hộp thoại NPC.")]
    [SerializeField] private AudioSource typingAudioSource;
    [Tooltip("Tốc độ gõ chữ (giây/ký tự). Khớp với tốc độ hiển thị text bên NPCDialogue.")]
    [SerializeField] private float typingSpeed = 0.04f;
    [Tooltip("TextMeshProUGUI hiển thị nội dung trong hộp thoại NPC. Cần để đếm ký tự thật sự hiển thị.")]
    [SerializeField] private TextMeshProUGUI npcDialogueText;

    private static readonly HashSet<char> silentChars = new HashSet<char> { ' ', '\n', '\t' };
    private Coroutine typingSoundCoroutine;

    void Start()
    {
        StartCoroutine(Intro());
    }

    IEnumerator Intro()
    {
        // ================= MỎ ĐÁ (Highlight Gỗ/Đá khi nhắc đến) =================
        SetTimePaused(true);
        yield return ShowAndWaitWithSound("Chào mừng đến với vùng đất Khẩn Hoang! Hãy bắt đầu bằng việc khai thác tài nguyên.");

        ShowWithSound("Lia camera đến mỏ đá...");
        Camera.main.transform.position = stoneMineWorld.position;

        // Highlight Gỗ và Đá khi nhắc đến yêu cầu mở mỏ
        highlight.Highlight(woodIcon);
        highlight.Highlight(stoneIcon);
        yield return ShowAndWaitWithSound("Mỏ đá đang bị khóa. Cần 7 worker và 12 Gỗ để mở.");
        highlight.ClearAll(); // Tắt highlight sau khi nhắc

        SetTimePaused(false);
        yield return new WaitUntil(() => worker >= 7 && wood >= 12);

        SetTimePaused(true);
        yield return ShowAndWaitWithSound("Mỏ đá đã mở!");
        npc.Hide();
        Camera.main.transform.position = Vector3.zero;
        SetTimePaused(false);

        // ================= NGÀY THỨ 3 (CẢNH BÁO) =================
        yield return new WaitUntil(() => DayNightManager.Ins != null && DayNightManager.Ins.CurrentDay >= 3);
        SetTimePaused(true);
        yield return ShowAndWaitWithSound("Phó lý: Bẩm, địch đang chuẩn bị tấn công!");
        yield return ShowAndWaitWithSound("Hãy chuẩn bị 15 Gỗ và 10 Đá để mở tháp canh bảo vệ làng!");
        npc.Hide();
        SetTimePaused(false);

        // ================= NGÀY THỨ 4 (Highlight Gỗ/Đá khi nâng cấp) =================
        yield return new WaitUntil(() => DayNightManager.Ins != null && DayNightManager.Ins.CurrentDay >= 4);
        SetTimePaused(true);

        highlight.Highlight(woodIcon);
        highlight.Highlight(stoneIcon);
        yield return ShowAndWaitWithSound("Ngày thứ 4: Khi tích lũy đủ 40 Gỗ và 60 Đá, hãy nâng cấp nhà Worker.");
        highlight.ClearAll();

        yield return ShowAndWaitWithSound("Việc này sẽ mở khóa công nghệ Tháp Canh để chống địch!");
        npc.Hide();
        SetTimePaused(false);

        // ================= NGÀY THỨ 5 (BUILD THÁP CANH) =================
        yield return new WaitUntil(() => DayNightManager.Ins != null && DayNightManager.Ins.CurrentDay >= 5);
        SetTimePaused(true);

        if (openBuildPanelBtn != null) highlight.Highlight(openBuildPanelBtn);
        yield return ShowAndWaitWithSound("Hãy mở Menu Xây Dựng để chế tạo hệ thống phòng thủ.");
        yield return new WaitUntil(() => buildPanelObject != null && buildPanelObject.activeSelf == true);
        highlight.ClearAll();

        if (buildWatchTowerBtn != null)
        {
            highlight.Highlight(buildWatchTowerBtn);
            buildWatchTowerBtn.GetComponent<Button>().onClick.AddListener(OnWatchTowerBtnClicked);
        }
        yield return ShowAndWaitWithSound("Hãy chọn xây Tháp Canh (Watch Tower)!");
        yield return new WaitUntil(() => isWatchTowerButtonClicked == true);
        highlight.ClearAll();
        npc.Hide();
        SetTimePaused(false);

        // ================= NGÀY THỨ 6 (XÂY NHÀ BẾP - Highlight Lúa) =================
        yield return new WaitUntil(() => DayNightManager.Ins != null && DayNightManager.Ins.CurrentDay >= 6);
        SetTimePaused(true);

        Camera.main.transform.position = newForestWorld.position;
        highlight.Highlight(foodIcon); // Highlight Lúa khi nhắc đến Nhà Bếp
        yield return ShowAndWaitWithSound("Gỗ đã cạn! Hãy xây Nhà Bếp dùng LÚA để hồi năng lượng cho dân làng.");
        highlight.ClearAll();

        if (openBuildPanelBtn != null) highlight.Highlight(openBuildPanelBtn);
        yield return new WaitUntil(() => buildPanelObject != null && buildPanelObject.activeSelf == true);
        highlight.ClearAll();

        if (buildKitchenBtn != null)
        {
            highlight.Highlight(buildKitchenBtn);
            buildKitchenBtn.GetComponent<Button>().onClick.AddListener(OnKitchenBtnClicked);
        }
        yield return ShowAndWaitWithSound("Hãy chọn xây Nhà Bếp (Kitchen)!");
        yield return new WaitUntil(() => isKitchenButtonClicked == true);
        highlight.ClearAll();

        Camera.main.transform.position = Vector3.zero;
        npc.Hide();
        SetTimePaused(false);

        // ================= NGÀY THỨ 8 (ĐỢT TẤN CÔNG LỚN) =================
        yield return new WaitUntil(() => DayNightManager.Ins != null && DayNightManager.Ins.CurrentDay >= 8);
        SetTimePaused(true);

        yield return ShowAndWaitWithSound("Ngày thứ 8: Địch Phi Lao và Bắn Cung đang mở một đợt tấn công lớn!");

        if (watchTowerWorld != null)
        {
            ShowWithSound("Lia camera đến tháp canh...");
            Camera.main.transform.position = watchTowerWorld.position;
        }
        yield return ShowAndWaitWithSound("Tháp canh đang liên tục quét vị trí kẻ địch, hãy ra lệnh cho tháp phòng thủ bắn trả!");

        if (alliedTroopsWorld != null)
        {
            ShowWithSound("Lia camera đến lính ta...");
            Camera.main.transform.position = alliedTroopsWorld.position;
        }
        yield return ShowAndWaitWithSound("Huấn luyện thêm đội Lính Ta và ra lệnh tiến lên chống trả để giành chiến thắng!");

        Camera.main.transform.position = Vector3.zero;
        npc.Hide();
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

        yield return ShowAndWaitWithSound("Địch đang chuẩn bị tổng lực tấn công!");
        yield return ShowAndWaitWithSound("Cậu hãy xây dựng pháo và cung để chống trả!");

        if (WarningUI.Instance != null)
        {
            WarningUI.Instance.Hide();
        }

        Camera.main.transform.position = Vector3.zero;

        if (buildPanelObject != null && buildPanelObject.activeSelf == false)
        {
            if (openBuildPanelBtn != null) highlight.Highlight(openBuildPanelBtn);
            yield return ShowAndWaitWithSound("Hãy mở Menu Xây Dựng để bố trí vũ khí!");
            yield return new WaitUntil(() => buildPanelObject.activeSelf == true);
            highlight.ClearAll();
        }

        if (buildCannonBtn != null)
        {
            Button btnCannon = buildCannonBtn.GetComponent<Button>();
            if (btnCannon != null) { btnCannon.onClick.RemoveListener(OnCannonBtnClicked); btnCannon.onClick.AddListener(OnCannonBtnClicked); }
            highlight.Highlight(buildCannonBtn);
        }
        yield return ShowAndWaitWithSound("Hãy chọn xây THÁP PHÁO (Cannon)!");
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
        yield return ShowAndWaitWithSound("Đồng thời xây thêm THÁP CUNG!");
        yield return new WaitUntil(() => isArcherButtonClicked == true);
        highlight.ClearAll();

        if (day9EnemySpawnPos != null) Camera.main.transform.position = day9EnemySpawnPos.position;
        yield return ShowAndWaitWithSound("Kìa, địch đang di chuyển qua! Hãy phối hợp cùng quân lính bảo vệ ngôi làng và tiến lên san phẳng căn cứ địch!");

        Camera.main.transform.position = Vector3.zero;
        npc.Hide();
        SetTimePaused(false); // Cho đồng hồ chạy lại và trận chiến diễn ra!

        yield return new WaitUntil(() => CheckEnemyCampDestroyed());

        SetTimePaused(true); // Dừng lại để chúc mừng
        yield return ShowAndWaitWithSound("Xuất sắc! Căn cứ điểm của bọn Thổ phỉ đã bị san phẳng hoàn toàn!");
        yield return ShowAndWaitWithSound("Phần thưởng chiến công: +500 xu vàng đã được chuyển vào kho báu của làng!");

        ClearAllMapFog();
        yield return ShowAndWaitWithSound("Toàn bộ sương mù bao phủ vùng đất đã tan biến! Hãy chuẩn bị cho trận chiến cuối cùng.");
        npc.Hide();
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

        yield return ShowAndWaitWithSound("NGÀY 10 - TRẬN CHIẾN QUYẾT ĐỊNH VẬN MỆNH VÙNG ĐẤT!");
        yield return ShowAndWaitWithSound("Thủ lĩnh tối cao Thổ phỉ (BOSS CUỐI) đang tiến vào! Hãy dồn toàn bộ lực lượng tiêu diệt hắn!");

        if (WarningUI.Instance != null)
        {
            WarningUI.Instance.Hide();
        }

        npc.Hide();
        SetTimePaused(false);

        yield return new WaitUntil(() => CheckBossDefeated() || DayNightManager.Ins.CurrentDay > 10);

        SetTimePaused(true);
        yield return ShowAndWaitWithSound("Tuyệt vời! Bạn đã đánh bại thủ lĩnh tối cao và bảo vệ vùng đất Khẩn Hoang thành công rực rỡ!");
        yield return ShowAndWaitWithSound("Bình minh ló rạng, cuộc sống của dân làng từ nay đã được ấm no, bình yên ổn định...");
        yield return ShowAndWaitWithSound("Bạn đã giữ trọn vẹn lời hứa thiêng liêng với vị cố Trưởng Làng quá cố!");

        UnlockEndlessMode();

        yield return ShowAndWaitWithSound("HỆ THỐNG: Chúc mừng bạn đã hoàn thành mạch truyện chính! Đã mở khóa CHẾ ĐỘ CHƠI VÔ TẬN.");
        yield return ShowAndWaitWithSound("Giờ đây bạn có thể tiếp tục phát triển, xây dựng và sinh tồn không giới hạn tại vùng đất này!");
        npc.Hide();

        highlight.ClearAll();
        SetTimePaused(false);
    }

    // =========================================================
    //  TYPING SOUND HELPERS
    // =========================================================

    /// <summary>
    /// Thay thế npc.ShowAndWait() — hiện text + chạy typing sound song song, rồi chờ hộp thoại đóng.
    /// </summary>
    private IEnumerator ShowAndWaitWithSound(string message)
    {
        StopTypingSound();
        typingSoundCoroutine = StartCoroutine(PlayTypingSound(message));
        yield return npc.ShowAndWait(message);
        StopTypingSound();
    }

    /// <summary>
    /// Thay thế npc.Show() — hiện text + chạy typing sound (không chờ, fire-and-forget).
    /// </summary>
    private void ShowWithSound(string message)
    {
        StopTypingSound();
        npc.Show(message);
        typingSoundCoroutine = StartCoroutine(PlayTypingSound(message));
    }

    private void StopTypingSound()
    {
        if (typingSoundCoroutine != null)
        {
            StopCoroutine(typingSoundCoroutine);
            typingSoundCoroutine = null;
        }
        if (typingAudioSource != null) typingAudioSource.Stop();
    }

    /// <summary>
    /// Phát âm thanh gõ chữ khớp từng ký tự hiển thị thật sự.
    /// Dùng npcDialogueText.GetParsedText() để bỏ qua rich-text tag (giống StoryUIController).
    /// Dùng AudioSettings.dspTime + PlayScheduled() để khớp chính xác với audio thread.
    /// </summary>
    private IEnumerator PlayTypingSound(string message)
    {
        if (typingAudioSource == null || typingAudioSource.clip == null) yield break;

        // Nếu có tham chiếu tới TextMeshPro của hộp thoại NPC, dùng GetParsedText()
        // để bỏ hết rich-text tag, đếm đúng ký tự thật sự hiển thị.
        string parsedText = message;
        if (npcDialogueText != null)
        {
            npcDialogueText.text = message;
            npcDialogueText.ForceMeshUpdate();
            parsedText = npcDialogueText.GetParsedText();
        }

        double nextDspTime = AudioSettings.dspTime;

        foreach (char c in parsedText)
        {
            if (!silentChars.Contains(c))
            {
                typingAudioSource.pitch = Random.Range(0.95f, 1.05f);
                typingAudioSource.Stop();
                typingAudioSource.PlayScheduled(nextDspTime);
            }
            yield return new WaitForSeconds(typingSpeed);
            nextDspTime = AudioSettings.dspTime;
        }
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