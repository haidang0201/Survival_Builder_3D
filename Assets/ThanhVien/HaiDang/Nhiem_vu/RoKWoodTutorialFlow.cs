using System.Collections;
using UnityEngine;

public class RoKWoodTutorialFlow : MonoBehaviour
{
    [Header("CORE")]
    public NPCDialogue npc;
    public UIHighlightSystem highlight;

    [Header("UI")]
    public RectTransform woodIcon;

    [Header("WORLD")]
    public Transform woodTree;

    [Header("WORKER")]
    public WorkerController worker;

    [Header("ARROW")]
    public GameObject arrowPrefab;
    GameObject arrow;

    bool stepContinue;
    bool continuePressed;

    void Start()
    {
        npc.onContinueEvent += OnContinue;
        StartCoroutine(Tutorial());
    }

    void OnDestroy()
    {
        npc.onContinueEvent -= OnContinue;
    }
    void OnContinue()
    {
        continuePressed = true;
    }

    IEnumerator Tutorial()
    {
        yield return npc.ShowAndWait("Bạn cần thu thập tài nguyên gỗ.");

        highlight.Highlight(woodIcon);

        // 👉 CHỜ BẤM CONTINUE
        yield return new WaitUntil(() => continuePressed);
        continuePressed = false;

        // 👉 TẮT UI + HIGHLIGHT
        npc.HideNow();
        highlight.ClearAll();

        // 👉 ARROW + WORKER
        SpawnArrow(woodTree.position);

        worker.MoveTo(woodTree);

        yield return new WaitUntil(() => worker.IsWorking);

        // ================= CLEAN =================
        DestroyArrow();

        yield return npc.ShowAndWait("Tốt! Bạn đã thu thập gỗ thành công.");
    }

    // =====================================================
    // CONTINUE BUTTON HOOK
    // =====================================================

    public void OnContinuePressed()
    {
        stepContinue = true;
    }

    IEnumerator WaitContinue()
    {
        stepContinue = false;
        yield return new WaitUntil(() => stepContinue);
    }

    // =====================================================
    // ARROW SYSTEM
    // =====================================================

    void SpawnArrow(Vector3 pos)
    {
        if (arrowPrefab == null) return;

        arrow = Instantiate(arrowPrefab);
        arrow.transform.position = pos + Vector3.up * 2f;
        arrow.transform.LookAt(pos);
    }

    void DestroyArrow()
    {
        if (arrow != null)
            Destroy(arrow);
    }
}