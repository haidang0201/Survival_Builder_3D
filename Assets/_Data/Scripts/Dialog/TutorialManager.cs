using System.Collections;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [Header("Core")]
    public NPCDialogue npc;
    public UIHighlightSystem highlight;

    [Header("Managers")]
    public ResourceMa ResourceMa;
    public CameraFocus cameraFocus;

    [Header("World Target")]
    public Transform stoneMineTarget;

    [Header("Tutorial Settings")]
    public bool runOnStart = true;
    public bool hideStoneMineAtStart = true;
    public bool debugKeys = true;

    public float shortDelay = 1.5f;
    public float mediumDelay = 2.2f;

    private Coroutine tutorialRoutine;

    void Start()
    {
        CacheReferences();

        if (hideStoneMineAtStart && stoneMineTarget != null)
            stoneMineTarget.gameObject.SetActive(false);

        if (runOnStart)
            tutorialRoutine = StartCoroutine(RunDay1Tutorial());
    }

    void Update()
    {
        if (!debugKeys) return;

        ResourceMa rm = GetResourceMa();

        if (rm == null) return;

        // Test nhanh:
        // Phím 1 = +5 gỗ
        // Phím 2 = +1 đá
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            rm.wood += 5;
            Debug.Log("[DEBUG] +5 wood");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            rm.stone += 1;
            Debug.Log("[DEBUG] +1 stone");
        }
    }

    private IEnumerator RunDay1Tutorial()
    {
        CacheReferences();

        if (npc == null)
        {
            Debug.LogError("[TUTORIAL] npc chưa gán.");
            yield break;
        }

        if (highlight == null)
        {
            Debug.LogError("[TUTORIAL] highlight chưa gán.");
            yield break;
        }

        // STEP 0 - Intro
        yield return npc.ShowAndWait(
            "Cậu đã đến rồi. Tốt lắm. Tôi là phó lý — người lo việc lớn nhỏ trong làng này."
        );

        // STEP 1 - Giới thiệu HUD gỗ
        highlight.HighlightWood();

        yield return npc.ShowAndWait(
            "Nhìn lên thanh tài nguyên. Đây là gỗ — thứ đầu tiên làng cần để sống."
        );

        // Người chơi bấm Tiếp tục xong mới tắt highlight
        highlight.ClearAll();

        // STEP 1B - Nhiệm vụ thu thập 5 gỗ
        highlight.HighlightWood();

        npc.ShowObjective(
            "Hãy thu thập đủ 5 gỗ đầu tiên."
        );

        yield return new WaitUntil(() => GetWood() >= 5);

        highlight.ClearAll();

        yield return npc.ShowAndWait(
            "Tốt. Làng đã có gỗ để bắt đầu dựng xây."
        );

        // STEP 2 - Worker có sẵn
        highlight.HighlightWorker();

        yield return npc.ShowAndWait(
            "Cậu không cô độc. Làng đã có 2 người thợ sẵn sàng làm việc."
        );

        highlight.ClearAll();

        // STEP 3 - Mở mỏ đá
        yield return npc.ShowAndWait(
            "Nhưng gỗ thôi chưa đủ. Phía xa kia có một mỏ đá."
        );

        UnlockStoneMine();

        yield return null;

        FocusStoneMine();

        yield return npc.ShowAndWait(
            "Đó là nơi cậu sẽ khai thác đá."
        );

        // STEP 4 - Giới thiệu đá trên HUD
        highlight.HighlightStone();

        yield return npc.ShowAndWait(
            "Đá sẽ giúp cậu xây những công trình chắc chắn hơn."
        );

        // Người chơi bấm Tiếp tục xong mới tắt highlight
        highlight.ClearAll();

        // STEP 4B - Nhiệm vụ khai thác đá
        highlight.HighlightStone();

        npc.ShowObjective(
            "Hãy khai thác đá đầu tiên."
        );

        yield return new WaitUntil(() => GetStone() > 0);

        highlight.ClearAll();

        yield return npc.ShowAndWait(
            "Tốt lắm. Gỗ và đá đã bắt đầu chảy về làng. Từ đây, thành của cậu mới thật sự sống."
        );

        highlight.ClearAll();
    }

    private void CacheReferences()
    {
        if (ResourceMa == null)
            ResourceMa = FindFirstObjectByType<ResourceMa>();

        if (cameraFocus == null)
            cameraFocus = FindFirstObjectByType<CameraFocus>();

        if (stoneMineTarget == null)
        {
            GameObject mine = GameObject.Find("StoneMine");

            if (mine != null)
                stoneMineTarget = mine.transform;
        }
    }

    private ResourceMa GetResourceMa()
    {
        if (ResourceMa == null)
            ResourceMa = FindFirstObjectByType<ResourceMa>();

        return ResourceMa;
    }

    private int GetWood()
    {
        ResourceMa rm = GetResourceMa();

        if (rm == null)
            return 0;

        return rm.wood;
    }

    private int GetStone()
    {
        ResourceMa rm = GetResourceMa();

        if (rm == null)
            return 0;

        return rm.stone;
    }

    private void UnlockStoneMine()
    {
        if (stoneMineTarget == null)
        {
            Debug.LogWarning("[TUTORIAL] stoneMineTarget chưa gán.");
            return;
        }

        stoneMineTarget.gameObject.SetActive(true);
    }

    private void FocusStoneMine()
    {
        if (stoneMineTarget == null)
        {
            Debug.LogWarning("[TUTORIAL] Không có StoneMine để lia camera.");
            return;
        }

        if (cameraFocus == null)
        {
            Debug.LogWarning("[TUTORIAL] cameraFocus chưa gán.");
            return;
        }

        cameraFocus.MoveTo(stoneMineTarget);
    }
}