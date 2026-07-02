using System.Collections;
using UnityEngine;

public class RoKCinematicTutorialController : MonoBehaviour
{
    [Header("CORE")]
    public NPCDialogue npc;
    public UIHighlightSystem highlight;
    public RoKQuestPanelUI questPanel;

    [Header("UI TARGETS")]
    public RectTransform questOpenIcon;
    public RectTransform firstQuestItem;
    public RectTransform goButton;

    [Header("WORLD TARGETS")]
    public Transform buildWorldPoint;
    public Transform resourceWorldPoint;
    public Transform soldierWorldPoint;

    [Header("CAMERA")]
    public Camera mainCamera;
    public Vector3 cameraOffset = new Vector3(0, 10, -10);
    public float cameraSpeed = 2f;

    [Header("STATE")]
    public bool questOpened;
    public bool goPressed;

    bool tutorialStarted;

    void Start()
    {
        if (questPanel != null)
        {
            questPanel.onGoPressed.RemoveListener(OnQuestGo);
            questPanel.onGoPressed.AddListener(OnQuestGo);
        }

        StartCoroutine(TutorialFlow());
    }

    public void OnQuestGo(string questId)
    {
        Debug.Log("[Tutorial] QUEST GO: " + questId);

        if (questId == "build_first_building")
            goPressed = true;

        if (questId == "gather_resource")
            goPressed = true;

        if (questId == "train_soldier")
            goPressed = true;
    }

    // =====================================================
    // MAIN FLOW
    // =====================================================

    IEnumerator TutorialFlow()
    {
        if (tutorialStarted) yield break;
        tutorialStarted = true;

        // ================= STEP 0 =================
        yield return npc.ShowAndWait("Chào mừng đến với lãnh địa!");

        // ================= STEP 1 =================
        highlight.Highlight(questOpenIcon);
        yield return npc.ShowAndWait("Đây là bảng nhiệm vụ. Hãy mở nó.");

        yield return new WaitUntil(() => questOpened);

        highlight.ClearAll();

        // mở quest panel
        if (questPanel != null)
            questPanel.OpenPanel();

        // ================= STEP 2 =================
        highlight.Highlight(firstQuestItem);
        yield return npc.ShowAndWait("Đây là nhiệm vụ đầu tiên. Hãy chú ý.");

        // ================= STEP 3 =================
        highlight.Highlight(goButton);
        yield return npc.ShowAndWait("Nhấn nút Đi để bắt đầu nhiệm vụ.");

        yield return new WaitUntil(() => goPressed);

        highlight.ClearAll();

        // ================= STEP 4 (CINEMATIC START) =================
        yield return npc.ShowAndWait("Bắt đầu nhiệm vụ!");

        if (buildWorldPoint != null)
            yield return MoveCamera(buildWorldPoint);

        yield return npc.ShowAndWait("Hãy xây công trình đầu tiên để phát triển lãnh địa.");

        // ================= NEXT =================
        yield return npc.ShowAndWait("Tutorial hoàn tất bước đầu tiên.");
    }

    // =====================================================
    // CAMERA
    // =====================================================

    IEnumerator MoveCamera(Transform target)
    {
        if (target == null) yield break;

        Vector3 start = mainCamera.transform.position;
        Vector3 end = target.position + cameraOffset;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * cameraSpeed;
            mainCamera.transform.position = Vector3.Lerp(start, end, t);
            mainCamera.transform.LookAt(target);
            yield return null;
        }
    }

    // =====================================================
    // UI EVENTS (IMPORTANT FIX)
    // =====================================================

    public void OnQuestOpen()
    {
        questOpened = true;
    }

    public void OnGoPressed()
    {
        goPressed = true;
    }
}