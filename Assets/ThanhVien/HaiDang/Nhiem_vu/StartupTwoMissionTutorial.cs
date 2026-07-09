using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class StartupTwoMissionTutorial : MonoBehaviour
{
    public static StartupTwoMissionTutorial Instance { get; private set; }

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

        yield return Say("Nhiệm vụ đầu tiên: hãy xây Tháp Canh để phát hiện kẻ địch từ xa.");

        yield return Step_OpenBuildPanel();

        yield return Step_SelectWatchTower();

        yield return Step_PlaceWatchTower();

        yield return Say("Tốt lắm. Tháp Canh đã được đặt xong.");

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
            yield return MoveCameraTo(towerPlacePoint);

        if (worldArrow != null && towerPlacePoint != null)
            worldArrow.Show(towerPlacePoint);

        // Không hiện nút Tiếp tục. Người chơi phải đặt Tháp Canh xuống đất.
        ShowObjective("Đặt Tháp Canh vào vị trí mũi tên chỉ dẫn.");

        yield return new WaitUntil(() => watchTowerPlaced);
        yield return Say(
    "Tốt lắm. Tháp Canh đã được đặt xong."
);

        IsWaitingForWatchTowerPlacement = false;

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
    public void NotifyWatchTowerPlaced()
    {
        if (!IsWaitingForWatchTowerPlacement)
            return;


        watchTowerPlaced = true;

        IsWaitingForWatchTowerPlacement = false;


        Debug.Log(
            "[StartupTwoMissionTutorial] Tháp Canh xây hoàn thành."
        );
    }
    IEnumerator WatchTowerCompleteDialog()
    {
        yield return Say(
            "Tốt lắm. Tháp Canh đã được đặt xong."
        );

        yield return new WaitForSeconds(0.5f);

        HideNPC();
    }

    // public void TestPlaced()
    // {
    //     NotifyWatchTowerPlaced();
    // }

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

    IEnumerator MoveCameraTo(Transform target)
    {
        if (mainCamera == null || target == null)
            yield break;

        Vector3 start = mainCamera.transform.position;
        Vector3 end = target.position + cameraOffset;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / cameraMoveTime;
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            mainCamera.transform.position = Vector3.Lerp(start, end, smooth);
            yield return null;
        }
    }
}