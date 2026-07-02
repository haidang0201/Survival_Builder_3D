using System.Collections;
using UnityEngine;

public class RoKTutorialCoreV2 : MonoBehaviour
{
    [Header("NPC")]
    public NPCDialogue npc;

    [Header("HIGHLIGHT")]
    public UIHighlightSystem highlight;

    [Header("CAMERA")]
    public Camera mainCamera;
    public Vector3 offset = new Vector3(0, 10, -10);
    public float moveSpeed = 2f;

    [Header("UI TARGETS")]
    public RectTransform buildButton;
    public RectTransform resourceIcon;
    public RectTransform questButton;

    [Header("WORLD")]
    public Transform buildPoint;
    public Transform resourcePoint;

    [Header("STATE")]
    bool buildDone;
    bool resourceDone;
    bool questPressed;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        StartCoroutine(RunTutorial());
    }

    IEnumerator RunTutorial()
    {
        // ================= INTRO =================
        yield return npc.ShowAndWait("Chào mừng đến vương quốc!");

        // ================= QUEST 1 (DIRECT) =================
        yield return Quest1_Build();

        // ================= QUEST 2 (DIRECT) =================
        yield return Quest2_Resource();

        // ================= QUEST 3 (PANEL MODE) =================
        yield return Quest3_Panel();
    }

    // =====================================================
    // QUEST 1 - BUILD (DIRECT NPC GUIDE)
    // =====================================================

    IEnumerator Quest1_Build()
    {
        yield return npc.ShowAndWait("Hãy xây công trình đầu tiên.");

        highlight.Highlight(buildButton);

        yield return npc.ShowAndWait("Nhấn nút xây dựng.");

        yield return new WaitUntil(() => buildDone);

        highlight.ClearAll();

        yield return npc.ShowAndWait("Tốt! Công trình đã hoàn thành.");
    }

    // =====================================================
    // QUEST 2 - RESOURCE (DIRECT NPC GUIDE)
    // =====================================================

    IEnumerator Quest2_Resource()
    {
        yield return npc.ShowAndWait("Giờ hãy thu thập tài nguyên.");

        if (resourcePoint != null)
            yield return MoveCamera(resourcePoint);

        highlight.Highlight(resourceIcon);

        yield return npc.ShowAndWait("Gửi dân đi khai thác.");

        yield return new WaitUntil(() => resourceDone);

        highlight.ClearAll();

        yield return npc.ShowAndWait("Rất tốt! Bạn đã có tài nguyên.");
    }

    // =====================================================
    // QUEST 3 - QUEST PANEL MODE
    // =====================================================

    IEnumerator Quest3_Panel()
    {
        yield return npc.ShowAndWait("Giờ hãy dùng bảng nhiệm vụ.");

        highlight.Highlight(questButton);

        yield return npc.ShowAndWait("Mở bảng nhiệm vụ để tiếp tục.");

        yield return new WaitUntil(() => questPressed);

        highlight.ClearAll();

        yield return npc.ShowAndWait("Tutorial cơ bản hoàn tất.");
    }

    // =====================================================
    // CAMERA MOVE
    // =====================================================

    IEnumerator MoveCamera(Transform target)
    {
        Vector3 start = mainCamera.transform.position;
        Vector3 end = target.position + offset;

        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * moveSpeed;
            mainCamera.transform.position = Vector3.Lerp(start, end, t);
            mainCamera.transform.LookAt(target);
            yield return null;
        }
    }

    // =====================================================
    // EXTERNAL EVENTS
    // =====================================================

    public void OnBuildComplete()
    {
        buildDone = true;
    }

    public void OnResourceComplete()
    {
        resourceDone = true;
    }

    public void OnQuestPanelOpened()
    {
        questPressed = true;
    }
}