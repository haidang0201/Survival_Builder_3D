using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

public class StartupTwoMissionTutorial : MonoBehaviour
{
    [Header("CORE")]
    public NPCDialogue npc;
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

    [Header("OPTIONS")]
    public bool autoStart = true;
    public float startDelay = 0.4f;

    Coroutine routine;

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

        HideArrow();
        ClearHighlight();

        yield return Say("Nhiệm vụ đầu tiên: hãy xây Tháp Canh để phát hiện kẻ địch từ xa.");

        yield return Step_OpenBuildPanel();

        yield return Step_SelectWatchTower();

        yield return Step_PlaceWatchTower();

        yield return Say("Tốt lắm. Tháp Canh đã được đặt xong.");

        HideNPC();
        ClearHighlight();
        HideArrow();
    }

    // =====================================================
    // STEP 1: BẮT BUỘC BẤM BUILD
    // =====================================================

    IEnumerator Step_OpenBuildPanel()
    {
        HighlightUI(openBuildButtonRT);

        // Quan trọng: dùng ShowObjective, KHÔNG dùng ShowAndWait
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

        // Không cần bấm Tiếp tục, phải bấm Tháp Canh
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
    // STEP 3: CHỈ VỊ TRÍ ĐẶT THÁP — KHÔNG CÒN HIGHLIGHT TRÒN
    // =====================================================

    IEnumerator Step_PlaceWatchTower()
    {
        // Fix quan trọng: tắt vòng highlight tròn trước
        ClearHighlight();

        if (moveCameraToPlacePoint && towerPlacePoint != null)
            yield return MoveCameraTo(towerPlacePoint);

        if (worldArrow != null && towerPlacePoint != null)
            worldArrow.Show(towerPlacePoint);

        // Không hiện nút Tiếp tục, phải đặt tháp
        ShowObjective("Đặt Tháp Canh vào vị trí mũi tên chỉ dẫn.");

        yield return new WaitUntil(() => watchTowerPlaced);

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
    }

    public void OnWatchTowerButtonClicked()
    {
        watchTowerSelected = true;
    }

    // Gọi hàm này từ build system khi người chơi đặt tháp xong
    public void NotifyWatchTowerPlaced()
    {
        watchTowerPlaced = true;
    }

    // Test nhanh bằng button inspector
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
        HideNPC();
    }

    void ShowObjective(string text)
    {
        if (npc == null) return;

        // Gọi ShowObjective nếu NPCDialogue có hàm này
        MethodInfo method = npc.GetType().GetMethod("ShowObjective", new[] { typeof(string) });

        if (method != null)
        {
            method.Invoke(npc, new object[] { text });
        }
        else
        {
            // Fallback nếu NPCDialogue bản cũ chưa có ShowObjective
            npc.Show(text);
        }
    }

    void HideNPC()
    {
        if (npc == null) return;

        MethodInfo method = npc.GetType().GetMethod("Hide");

        if (method != null)
            method.Invoke(npc, null);
    }

    void HighlightUI(RectTransform target)
    {
        if (highlight == null || target == null) return;

        // Hỗ trợ cả UIHighlightSystem bản có HighlightRT và bản có Highlight
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