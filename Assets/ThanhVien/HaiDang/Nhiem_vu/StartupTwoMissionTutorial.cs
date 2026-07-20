using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class StartupTwoMissionTutorial : MonoBehaviour
{
    public static StartupTwoMissionTutorial Instance { get; private set; }
    [Header("References")]
    public BuildingCtrl watchTowerCtrl; // Kéo prefab tháp canh thật vào đây 

    [Header("CORE")]
    public RoKNpcMissionDialogUI npc;
    public UIHighlightSystem highlight;

    [Header("BUILD UI")]
    public Button openBuildButton;
    public RectTransform openBuildButtonRT;
    public GameObject buildPanelRoot;

    [Header("WATCH TOWER UI")]
    public Button watchTowerButton;
    public RectTransform watchTowerButtonRT;

    [Header("PLACE TARGET")]
    public Transform towerPlacePoint;
    public WorldTutorialArrow worldArrow;

    [Header("CAMERA")]
    public Camera mainCamera;
    public bool moveCameraToPlacePoint = true;
    public Vector3 cameraOffset = new Vector3(0, 10, -10);
    public float cameraMoveTime = 1.2f;

    [Header("ENEMY ALERT INTRO (MỚI)")]
    [Tooltip("Bật/tắt đoạn 'bất ngờ' báo kẻ địch xuất hiện ngay sau lời chào mừng, trước khi hướng dẫn xây Tháp Canh.")]
    public bool showEnemyAlertIntro = true;
    [Tooltip("Điểm mà camera sẽ lia tới để cho người chơi thấy kẻ địch đang di chuyển qua. Kéo 1 Transform đặt tại vị trí đó vào đây.")]
    public Transform enemySightPoint;
    [Tooltip("Offset camera khi lia tới enemySightPoint (giống cameraOffset nhưng tách riêng để tuỳ chỉnh góc nhìn khác nếu muốn).")]
    public Vector3 enemyCameraOffset = new Vector3(0, 10, -10);
    [Tooltip("Độ trễ nhỏ sau khi banner cảnh báo hiện lên rồi mới bắt đầu lia camera, cho người chơi kịp đọc dòng chữ.")]
    public float enemyAlertShowDelay = 0.3f;
    [Tooltip("Thời gian camera lia sang chỗ kẻ địch (giây).")]
    public float enemyCameraPanTime = 1.2f;
    [Tooltip("Thời gian camera dừng lại ở chỗ kẻ địch trước khi quay về (giây).")]
    public float enemyPanHoldSeconds = 2.5f;
    [Tooltip("Thời gian camera lia quay trở lại vị trí ban đầu (giây).")]
    public float cameraReturnMoveTime = 1.2f;

    [Tooltip("Transform của con kẻ địch ĐANG DI CHUYỂN để camera bám theo trong lúc giữ (enemyPanHoldSeconds). " +
             "Nếu để trống, sẽ dùng hành vi cũ: camera đứng yên tại enemySightPoint trong suốt thời gian giữ.")]
    public Transform enemyMovingTarget;
    [Tooltip("Tốc độ làm mượt khi camera bám theo kẻ địch di chuyển (càng lớn camera bám càng sát/nhanh).")]
    public float enemyFollowSmoothSpeed = 5f;

    [Header("STATE")]
    public bool buildButtonClicked;
    public bool watchTowerSelected;
    public bool watchTowerPlaced;

    [Header("READ ONLY")]
    public bool IsWaitingForWatchTowerPlacement;

    [Header("OPTIONS")]
    public bool autoStart = true;
    public float startDelay = 0.4f;

    [Header("QUEST LINK")]
    public RoKQuestPanelUI questPanel;
    public RoKQuestMissionGuideRouter questRouter;
    public RoKQuestCompletePopupUI completePopup;

    public string watchTowerQuestId = "build_watchtower";
    public bool showCompletePopup = true;
    public bool openQuestPanelAfterComplete = false;


    Coroutine routine;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        BindButtons();

        if (autoStart)
            routine = StartCoroutine(RunTutorial());
    }

    void OnDestroy()
    {
        UnbindButtons();

        if (Instance == this)
            Instance = null;
    }

    void BindButtons()
    {
        if (openBuildButton != null)
        {
            openBuildButton.onClick.RemoveListener(OnBuildButtonClicked);
            openBuildButton.onClick.AddListener(OnBuildButtonClicked);
        }

        if (watchTowerButton != null)
        {
            watchTowerButton.onClick.RemoveListener(OnWatchTowerButtonClicked);
            watchTowerButton.onClick.AddListener(OnWatchTowerButtonClicked);
        }
    }

    void UnbindButtons()
    {
        if (openBuildButton != null)
            openBuildButton.onClick.RemoveListener(OnBuildButtonClicked);

        if (watchTowerButton != null)
            watchTowerButton.onClick.RemoveListener(OnWatchTowerButtonClicked);
    }

    public IEnumerator RunTutorial()
    {
        yield return new WaitForSeconds(startDelay);

        buildButtonClicked = false;
        watchTowerSelected = false;
        watchTowerPlaced = false;
        IsWaitingForWatchTowerPlacement = false;

        HideArrow();
        ClearHighlight();

        // ===== LỜI CHÀO MỪNG (MỚI) =====
        yield return Say("Chào mừng tân trưởng làng đến với thuộc đia của chúng ta!");
        yield return Say("Ta là Phó Lý, sẽ luôn ở bên hướng dẫn cậu trong những bước đầu tiên.");
        yield return Say("Trước tiên, chúng ta cần một đôi mắt canh gác cho vùng đất này.");
        // ================================

        // ===== BẤT NGỜ: KẺ ĐỊCH XUẤT HIỆN (MỚI) =====
        // Ngay sau lời chào mừng, giới thiệu xong vùng đất -> bất ngờ hiện banner
        // cảnh báo kẻ địch, lia camera cho người chơi thấy kẻ địch đang di chuyển
        // qua, giữ vài giây rồi lia camera quay trở lại để tiếp tục hướng dẫn xây
        // Tháp Canh như bình thường.
        yield return Step_EnemyAlertIntro();
        // ================================

        yield return Say("Nhiệm vụ đầu tiên: hãy xây Tháp Canh để phát hiện kẻ địch từ xa.");

        yield return Step_OpenBuildPanel();

        yield return Step_SelectWatchTower();

        yield return Step_PlaceWatchTower();

        //yield return Say("Tốt lắm. Tháp Canh đã được đặt xong.");

        MarkWatchTowerQuestCompleteForClaim();

        HideNPC();
        ClearHighlight();
        HideArrow();
    }



    void MarkWatchTowerQuestCompleteForClaim()
    {
        // Ưu tiên báo qua router quản lý nhiệm vụ
        if (questRouter != null)
        {
            questRouter.ExternalCompleteWatchTowerFromStartup(false);
        }
        else if (questPanel != null)
        {
            questPanel.CompleteQuest(watchTowerQuestId);
        }

        // Popup kiểu Rise of Kingdom
        if (showCompletePopup && completePopup != null)
        {
            completePopup.Show(
                "Nhiệm vụ hoàn thành",
                "Tháp Canh đã được xây dựng. Hãy vào bảng nhiệm vụ để nhận thưởng.",
                () =>
                {
                    if (questPanel != null)
                        questPanel.OpenPanel();
                }
            );

            return;
        }

        // Hoặc mở thẳng bảng nhiệm vụ
        if (openQuestPanelAfterComplete && questPanel != null)
            questPanel.OpenPanel();

        Debug.Log("[StartupTwoMissionTutorial] Đã hoàn thành quest build_watchtower. Chờ người chơi vào bảng nhiệm vụ nhận thưởng.");
    }


    // =====================================================
    // STEP 0 (MỚI): BẤT NGỜ BÁO KẺ ĐỊCH XUẤT HIỆN + LIA CAMERA
    // =====================================================

    IEnumerator Step_EnemyAlertIntro()
    {
        if (!showEnemyAlertIntro)
            yield break;

        // Lưu lại vị trí camera hiện tại (trước khi lia đi) để lát quay lại đúng chỗ.
        Vector3 cameraStartPos = mainCamera != null ? mainCamera.transform.position : Vector3.zero;
        bool hasCamera = mainCamera != null;

        // Hiện banner cảnh báo "Kẻ địch xuất hiện!" một cách bất ngờ.
        if (RoKEnemyAlertUI.Instance != null)
        {
            RoKEnemyAlertUI.Instance.ShowAlert();
        }
        else
        {
            Debug.LogWarning("[StartupTwoMissionTutorial] Không tìm thấy RoKEnemyAlertUI.Instance trong scene -> bỏ qua banner cảnh báo (vẫn tiếp tục lia camera nếu có enemySightPoint).");
        }

        if (enemyAlertShowDelay > 0f)
            yield return new WaitForSeconds(enemyAlertShowDelay);

        // Lia camera sang chỗ kẻ địch đang di chuyển qua.
        if (hasCamera && enemySightPoint != null)
            yield return MoveCameraTo(enemySightPoint, enemyCameraOffset, enemyCameraPanTime);

        // Ngay khi camera đã lia tới nơi (thấy kẻ địch rồi) thì tắt banner cảnh báo
        // "Kẻ địch xuất hiện! Chuẩn bị phản công!" đi, không để nó che màn hình
        // trong lúc người chơi đang xem kẻ địch di chuyển.
        // Dùng SetSuppressed(true) thay vì chỉ HideAlert() để KHOÁ banner luôn,
        // tránh trường hợp một hệ thống khác (vd WatchTowerAI thật phát hiện kẻ
        // địch demo đang đi ngang) tự gọi ShowAlert() đè lên trong lúc cutscene.
        if (RoKEnemyAlertUI.Instance != null)
            RoKEnemyAlertUI.Instance.SetSuppressed(true);

        // Giữ camera lại trong enemyPanHoldSeconds giây:
        // - Nếu có enemyMovingTarget -> camera BÁM THEO kẻ địch đang di chuyển qua,
        //   để người chơi thấy nó đi được 1 đoạn chứ không đứng hình 1 chỗ.
        // - Nếu không gán enemyMovingTarget -> giữ nguyên hành vi cũ (đứng yên).
        if (enemyPanHoldSeconds > 0f)
        {
            if (hasCamera && enemyMovingTarget != null)
                yield return FollowEnemyForDuration(enemyMovingTarget, enemyCameraOffset, enemyPanHoldSeconds);
            else
                yield return new WaitForSeconds(enemyPanHoldSeconds);
        }

        // Banner vẫn đang bị khoá (SetSuppressed) từ lúc camera tới nơi, nên chắc
        // chắn đang ẩn ở đây rồi, không cần gọi HideAlert() thêm lần nữa.

        // Lia camera quay trở lại đúng vị trí ban đầu để tiếp tục hướng dẫn người
        // chơi mở bảng xây dựng, chọn và đặt Tháp Canh theo mũi tên như bình thường.
        if (hasCamera)
            yield return MoveCameraToPosition(cameraStartPos, cameraReturnMoveTime);

        // LƯU Ý: KHÔNG mở khoá (SetSuppressed(false)) lại nữa ở bất kỳ đâu. Từ
        // lúc camera lia về đây trở đi, banner cảnh báo kẻ địch sẽ TẮT VĨNH VIỄN,
        // không hiện lại nữa dù hệ thống phát hiện thật (WatchTowerAI...) có gọi
        // ShowAlert() bao nhiêu lần đi nữa.
    }

    // =====================================================
    // STEP 1: BẮT BUỘC BẤM BUILD
    // =====================================================

    IEnumerator Step_OpenBuildPanel()
    {
        HighlightUI(openBuildButtonRT);

        // Không hiện nút Tiếp tục. Người chơi phải bấm nút Build.
        ShowObjective("Hãy mở bảng xây dựng.");

        if (openBuildButton != null)
        {
            yield return new WaitUntil(() => buildButtonClicked);
        }
        else if (buildPanelRoot != null)
        {
            yield return new WaitUntil(() => buildPanelRoot.activeInHierarchy);
        }
        else
        {
            Debug.LogWarning("[WatchTowerTutorial] Chưa gán openBuildButton hoặc buildPanelRoot.");
            yield break;
        }

        if (buildPanelRoot != null)
            yield return new WaitUntil(() => buildPanelRoot.activeInHierarchy);

        HideNPC();
        ClearHighlight();
    }

    // =====================================================
    // STEP 2: CHỌN THÁP CANH
    // =====================================================

    IEnumerator Step_SelectWatchTower()
    {
        yield return Say("Tốt. Bây giờ hãy chọn công trình Tháp Canh.");

        HighlightUI(watchTowerButtonRT);

        // Không hiện nút Tiếp tục. Người chơi phải bấm Tháp Canh.
        ShowObjective("Chọn Tháp Canh trong bảng xây dựng.");

        if (watchTowerButton != null)
        {
            yield return new WaitUntil(() => watchTowerSelected);
        }
        else
        {
            Debug.LogWarning("[WatchTowerTutorial] Chưa gán watchTowerButton.");
            yield break;
        }

        HideNPC();
        ClearHighlight();
    }

    // =====================================================
    // STEP 3: CHỈ VỊ TRÍ ĐẶT THÁP
    // =====================================================

    IEnumerator Step_PlaceWatchTower()
    {
        // Tắt vòng highlight tròn. Bước này chỉ dùng mũi tên.
        ClearHighlight();

        watchTowerPlaced = false;
        IsWaitingForWatchTowerPlacement = true;

        if (moveCameraToPlacePoint && towerPlacePoint != null)
            yield return MoveCameraTo(towerPlacePoint, cameraOffset, cameraMoveTime);

        if (worldArrow != null && towerPlacePoint != null)
            worldArrow.Show(towerPlacePoint);

        // Không hiện nút Tiếp tục. Người chơi phải đặt Tháp Canh xuống đất.
        ShowObjective("Đặt Tháp Canh vào vị trí mũi tên chỉ dẫn.");

        Debug.Log("[StartupTwoMissionTutorial] Bắt đầu chờ watchTowerPlaced..."); // THÊM
        yield return new WaitUntil(() => watchTowerPlaced);
        Debug.Log("[StartupTwoMissionTutorial] watchTowerPlaced = true, chuẩn bị Say dialog Tốt lắm"); // THÊM

        IsWaitingForWatchTowerPlacement = false;

        yield return Say("Tốt lắm!\nTháp Canh đã được đặt xong.");
        Debug.Log("[StartupTwoMissionTutorial] Đã Say xong dialog Tốt lắm"); // THÊM

        HideNPC();
        HideArrow();
        ClearHighlight();
    }

    // =====================================================
    // BUTTON CALLBACKS
    // =====================================================

    public void OnBuildButtonClicked()
    {
        buildButtonClicked = true;
        Debug.Log("[StartupTwoMissionTutorial] Đã bấm nút mở bảng xây dựng.");
    }

    public void OnWatchTowerButtonClicked()
    {
        watchTowerSelected = true;
        Debug.Log("[StartupTwoMissionTutorial] Đã chọn Tháp Canh.");
    }

    // Gọi hàm này khi THÁP CANH THẬT được đặt xuống đất thành công
    // Gọi hàm này khi THÁP CANH THẬT được đặt xuống đất thành công
    // Gọi hàm này khi THÁP CANH THẬT được đặt xuống đất thành công
    // Gọi hàm này khi THÁP CANH THẬT được đặt xuống đất thành công
    // Gọi hàm này khi THÁP CANH THẬT được ĐẶT XUỐNG đất (chưa chắc xây xong)
    // -> do WatchTowerTutorialNotifier gọi lúc người chơi thả tháp xuống
    public void NotifyWatchTowerPlaced()
    {
        if (!IsWaitingForWatchTowerPlacement)
        {
            Debug.Log("[StartupTwoMissionTutorial] Đã đặt Tháp Canh nhưng tutorial chưa ở bước chờ đặt.");
            return;
        }

        Debug.Log("[StartupTwoMissionTutorial] Tutorial nhận tín hiệu: Tháp Canh đã đặt xuống, đang chờ xây xong.");

        // Tắt icon mũi tên NGAY khi đặt xuống đất
        if (worldArrow != null)
            worldArrow.Hide();

        // Ẩn dialog "Đặt Tháp Canh..." ngay lập tức. Trong lúc đang xây không hiện dialog nào cả.
        HideNPC();

        // KHÔNG set watchTowerPlaced ở đây nữa.
        // Chờ NotifyWatchTowerBuilt() được gọi khi tháp THẬT SỰ xây xong.
    }

    // Gọi hàm này khi THÁP CANH THẬT ĐÃ XÂY XONG HOÀN TOÀN (buildProgress = 1)
    // -> do chính BuildingCtrl.OnBuildComplete() của tháp vừa xây xong gọi, luôn đúng object thật
    public void NotifyWatchTowerBuilt()
    {
        if (!IsWaitingForWatchTowerPlacement)
        {
            Debug.Log("[StartupTwoMissionTutorial] Tháp Canh xây xong nhưng tutorial không còn chờ nữa (bỏ qua).");
            return;
        }

        Debug.Log("[StartupTwoMissionTutorial] Tháp Canh đã xây xong thật sự -> đánh dấu hoàn thành bước đặt tháp.");
        watchTowerPlaced = true;
    }

    // Coroutine đợi build xong rồi mới đánh dấu đặt tháp hoàn tất
    // private IEnumerator WaitAndShowDialog()
    // {
    //     yield return new WaitUntil(() => watchTowerCtrl != null && watchTowerCtrl.IsBuilt);
    //     watchTowerPlaced = true;
    // }
    // Coroutine đợi build xong


    // Coroutine đợi build xong
    // Coroutine đợi build xong
    // private IEnumerator WaitAndShowDialog()
    // {
    //     yield return new WaitUntil(() => watchTowerCtrl != null && watchTowerCtrl.buildProgress >= 1f);
    //     CompleteWatchTowerPlacement();
    // }

    // Đánh dấu đặt tháp hoàn tất + hiện thoại, chỉ được gọi đúng 1 lần khi tháp đã xây xong
    // void CompleteWatchTowerPlacement()
    // {
    //     watchTowerPlaced = true;
    //     StartCoroutine(Say("Tốt lắm!\nTháp Canh đã được đặt xong."));
    // }

    // Coroutine đợi build xong
    // private IEnumerator WaitAndShowDialog()
    // {
    //     yield return new WaitUntil(() => watchTowerCtrl != null && watchTowerCtrl.buildProgress >= 1f);
    //     StartCoroutine(Say("Tốt lắm!\nTháp Canh đã được đặt xong."));
    // }
    // IEnumerator WatchTowerCompleteDialog()
    // {
    //     yield return Say(
    //         "Tốt lắm. Tháp Canh đã được đặt xong."
    //     );

    //     yield return new WaitForSeconds(0.5f);

    //     HideNPC();
    // }

    public void TestPlaced()
    {
        NotifyWatchTowerPlaced();
    }

    // =====================================================
    // HELPERS
    // =====================================================

    IEnumerator Say(string text)
    {
        if (npc == null) yield break;

        yield return npc.ShowAndWait(text);
    }

    void ShowObjective(string text)
    {
        if (npc == null) return;

        npc.ShowObjective(text);
    }

    void HideNPC()
    {
        if (npc == null) return;

        npc.Hide();
    }

    void HighlightUI(RectTransform target)
    {
        if (highlight == null || target == null) return;

        MethodInfo method = highlight.GetType().GetMethod("HighlightRT", new[] { typeof(RectTransform) });

        if (method != null)
        {
            method.Invoke(highlight, new object[] { target });
            return;
        }

        method = highlight.GetType().GetMethod("Highlight", new[] { typeof(RectTransform) });

        if (method != null)
            method.Invoke(highlight, new object[] { target });
    }

    void ClearHighlight()
    {
        if (highlight != null)
            highlight.ClearAll();
    }

    void HideArrow()
    {
        if (worldArrow != null)
            worldArrow.Hide();
    }

    // Lia camera tới 1 Transform mục tiêu + offset + thời gian lia tuỳ chỉnh.
    // (Được tách offset/moveTime ra tham số riêng để Step_EnemyAlertIntro có thể
    // dùng chung hàm này với offset/thời gian khác so với lúc lia tới towerPlacePoint,
    // mà không ảnh hưởng gì tới hành vi cũ của Step_PlaceWatchTower.)
    IEnumerator MoveCameraTo(Transform target, Vector3 offset, float moveTime)
    {
        if (mainCamera == null || target == null)
            yield break;

        yield return MoveCameraToPosition(target.position + offset, moveTime);
    }

    // Lia camera thẳng tới 1 vị trí world cụ thể (dùng để lia camera quay trở lại
    // đúng vị trí ban đầu sau đoạn cảnh báo kẻ địch).
    IEnumerator MoveCameraToPosition(Vector3 endPos, float moveTime)
    {
        if (mainCamera == null)
            yield break;

        Vector3 start = mainCamera.transform.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / moveTime;
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            mainCamera.transform.position = Vector3.Lerp(start, endPos, smooth);
            yield return null;
        }

        // Đảm bảo chạm đúng vị trí đích, tránh sai số cộng dồn của Time.deltaTime.
        mainCamera.transform.position = endPos;
    }

    // Cho camera BÁM THEO 1 Transform (kẻ địch) đang di chuyển trong khoảng thời gian
    // duration, dùng thay cho việc đứng yên trong Step_EnemyAlertIntro để người chơi
    // thấy kẻ địch đi được 1 đoạn đường trước khi camera lia quay trở lại.
    IEnumerator FollowEnemyForDuration(Transform target, Vector3 offset, float duration)
    {
        if (mainCamera == null || target == null)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            Vector3 desiredPos = target.position + offset;

            // Lerp mượt theo thời gian thực (không phụ thuộc framerate) để camera
            // đuổi theo kẻ địch một cách tự nhiên chứ không "dính cứng" vào nó.
            mainCamera.transform.position = Vector3.Lerp(
                mainCamera.transform.position,
                desiredPos,
                1f - Mathf.Exp(-enemyFollowSmoothSpeed * Time.deltaTime)
            );

            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}