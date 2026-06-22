using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIHighlightSystem : MonoBehaviour
{
    [Header("HUD Canvas gốc")]
    public Canvas hudCanvas;

    [Header("Kéo icon HUD vào đây")]
    public RectTransform woodIconRT;
    public RectTransform stoneIconRT;
    public RectTransform foodIconRT;
    public RectTransform workerIconRT;
    public RectTransform buildButtonRT;
    public RectTransform dayTimerRT;
    public RectTransform enemyCounterRT;

    [Header("Pulse Settings")]
    public float pulseSpeed = 2.5f;
    public float pulseExtra = 14f;

    // ── Runtime ──────────────────────────────────────────
    private RectTransform trackedTarget;
    private GameObject spawnedRing;
    private RectTransform ringRT;
    private GameObject spawnedDim;
    private GameObject spawnedLockText;
    private Coroutine pulseRoutine;
    private Coroutine redRoutine;
    private Coroutine multiPulseRoutine;
    private List<Button> blockedButtons = new List<Button>();
    private List<GameObject> spawnedRings = new List<GameObject>();
    private RectTransform canvasRT;

    // ══════════════════════════════════════════════════════
    void Awake()
    {
        if (hudCanvas == null)
        {
            Debug.LogError("[HIGHLIGHT] ✗ hudCanvas NULL!");
            return;
        }
        canvasRT = hudCanvas.GetComponent<RectTransform>();
        Debug.Log($"<color=cyan>[HIGHLIGHT] Awake OK — {hudCanvas.name}</color>");
    }

    void LateUpdate()
    {
        if (trackedTarget == null || ringRT == null) return;
        ringRT.anchoredPosition = GetCanvasPos(trackedTarget);
    }

    // ══════════════════════════════════════════════════════
    //  SHORTCUT — gọi thẳng theo tên
    // ══════════════════════════════════════════════════════
    public void HighlightWood() => HighlightRT(woodIconRT, Color.yellow);
    public void HighlightStone() => HighlightRT(stoneIconRT, Color.yellow);
    public void HighlightFood() => HighlightRT(foodIconRT, Color.yellow);
    public void HighlightWorker() => HighlightRT(workerIconRT, Color.yellow);
    public void HighlightBuild() => HighlightRT(buildButtonRT, Color.yellow);
    public void HighlightDayTimer() => HighlightRT(dayTimerRT, new Color(1f, 0.6f, 0f));
    public void HighlightEnemy() => HighlightRedRT(enemyCounterRT);

    // ══════════════════════════════════════════════════════
    //  PUBLIC API
    // ══════════════════════════════════════════════════════
    public void HighlightUI(GameObject target)
    {
        if (target == null) return;
        HighlightRT(target.GetComponent<RectTransform>(), Color.yellow);
    }

    public void HighlightLocked(GameObject target)
    {
        if (target == null) return;
        HighlightLockedRT(target.GetComponent<RectTransform>());
    }

    public void HighlightRedWarning(GameObject target)
    {
        if (target == null) return;
        HighlightRedRT(target.GetComponent<RectTransform>());
    }

    public void HighlightMultiple(params GameObject[] targets)
    {
        ClearAll();
        CreateDim(new Color(0f, 0f, 0f, 0.45f));
        foreach (var t in targets)
        {
            if (t == null) continue;
            var rt = t.GetComponent<RectTransform>();
            if (rt != null) SpawnOneRing(rt, Color.yellow);
        }
        multiPulseRoutine = StartCoroutine(PulseMultipleRings());
    }

    public void BlockAllExcept(params GameObject[] allowed)
    {
        var allowSet = new HashSet<GameObject>(allowed);
        foreach (var btn in FindObjectsOfType<Button>(true))
        {
            if (!allowSet.Contains(btn.gameObject))
            {
                btn.interactable = false;
                blockedButtons.Add(btn);
            }
        }
    }

    public void UnblockAll()
    {
        foreach (var btn in blockedButtons)
            if (btn != null) btn.interactable = true;
        blockedButtons.Clear();
    }

    public void ClearAll()
    {
        if (pulseRoutine != null) { StopCoroutine(pulseRoutine); pulseRoutine = null; }
        if (redRoutine != null) { StopCoroutine(redRoutine); redRoutine = null; }
        if (multiPulseRoutine != null) { StopCoroutine(multiPulseRoutine); multiPulseRoutine = null; }

        if (spawnedRing != null) { Destroy(spawnedRing); spawnedRing = null; ringRT = null; }
        if (spawnedDim != null) { Destroy(spawnedDim); spawnedDim = null; }
        if (spawnedLockText != null) { Destroy(spawnedLockText); spawnedLockText = null; }

        foreach (var r in spawnedRings)
            if (r != null) Destroy(r);
        spawnedRings.Clear();

        trackedTarget = null;
    }

    // ══════════════════════════════════════════════════════
    //  INTERNAL HIGHLIGHT METHODS
    // ══════════════════════════════════════════════════════
    void HighlightRT(RectTransform target, Color ringColor)
    {
        Debug.Log($"<color=cyan>[HIGHLIGHT] HighlightRT → {target?.name}</color>");
        ClearAll();
        if (target == null) return;

        CreateDim(new Color(0f, 0f, 0f, 0.45f));
        CreateRing(target, ringColor);
        pulseRoutine = StartCoroutine(PulseRing());
    }

    void HighlightLockedRT(RectTransform target)
    {
        Debug.Log($"<color=cyan>[HIGHLIGHT] HighlightLocked → {target?.name}</color>");
        ClearAll();
        if (target == null) return;

        CreateDim(new Color(0f, 0f, 0f, 0.5f));
        CreateRing(target, new Color(1f, 0.3f, 0.3f));
        CreateLockLabel(target);
        pulseRoutine = StartCoroutine(PulseRing());
    }

    void HighlightRedRT(RectTransform target)
    {
        Debug.Log($"<color=red>[HIGHLIGHT] RedWarning → {target?.name}</color>");
        ClearAll();
        if (target == null) return;

        CreateDim(new Color(0.3f, 0f, 0f, 0.4f));
        CreateRing(target, new Color(1f, 0.15f, 0.15f));
        redRoutine = StartCoroutine(RedPulseRing());
    }

    // ══════════════════════════════════════════════════════
    //  TẠO UI ELEMENT TỪ CODE — KHÔNG CẦN PREFAB
    // ══════════════════════════════════════════════════════

    /// <summary>Tạo ring viền từ 4 Image mỏng (top/bottom/left/right)</summary>
    void CreateRing(RectTransform target, Color color)
    {
        trackedTarget = target;

        // FIX QUAN TRỌNG: Nếu target nằm trong 1 Layout Group (Horizontal/Vertical/Grid),
        // vị trí thật sự của nó chỉ được Layout Group tính toán xong ở giai đoạn riêng
        // (sau Update, trước LateUpdate) của Unity. Nếu CreateRing() chạy đúng frame mà
        // layout vừa thay đổi (panel cha vừa SetActive, vừa thêm/bớt phần tử...), việc đọc
        // GetWorldCorners() ngay lúc này có thể lấy phải vị trí CŨ (chưa kịp rebuild), khiến
        // ring bị vẽ lệch chỗ — đây chính là nguyên nhân gây lỗi "highlight sai vị trí".
        // Canvas.ForceUpdateCanvases() ép toàn bộ Canvas + Layout Group rebuild NGAY LẬP TỨC
        // trước khi mình đọc toạ độ, đảm bảo luôn lấy đúng vị trí đã ổn định.
        Canvas.ForceUpdateCanvases();

        Vector2 pos = GetCanvasPos(target);
        Vector2 size = GetSize(target) + Vector2.one * 20f;

        // Container
        spawnedRing = new GameObject("HL_Ring", typeof(RectTransform));
        spawnedRing.transform.SetParent(hudCanvas.transform, false);
        ringRT = spawnedRing.GetComponent<RectTransform>();
        ringRT.anchorMin = Vector2.zero;
        ringRT.anchorMax = Vector2.zero;
        ringRT.pivot = new Vector2(0.5f, 0.5f);
        ringRT.sizeDelta = size;
        ringRT.anchoredPosition = pos;
        spawnedRing.transform.SetAsLastSibling();

        float thickness = 3f;

        // Top
        CreateBar(spawnedRing.transform, "Top",
            new Vector2(0f, size.y * 0.5f - thickness * 0.5f),
            new Vector2(size.x, thickness), color);
        // Bottom
        CreateBar(spawnedRing.transform, "Bot",
            new Vector2(0f, -size.y * 0.5f + thickness * 0.5f),
            new Vector2(size.x, thickness), color);
        // Left
        CreateBar(spawnedRing.transform, "Left",
            new Vector2(-size.x * 0.5f + thickness * 0.5f, 0f),
            new Vector2(thickness, size.y), color);
        // Right
        CreateBar(spawnedRing.transform, "Right",
            new Vector2(size.x * 0.5f - thickness * 0.5f, 0f),
            new Vector2(thickness, size.y), color);

        // Corner dots trang trí (tuỳ chọn)
        float cs = 8f;
        CreateBar(spawnedRing.transform, "TL",
            new Vector2(-size.x * 0.5f, size.y * 0.5f), new Vector2(cs, cs), color);
        CreateBar(spawnedRing.transform, "TR",
            new Vector2(size.x * 0.5f, size.y * 0.5f), new Vector2(cs, cs), color);
        CreateBar(spawnedRing.transform, "BL",
            new Vector2(-size.x * 0.5f, -size.y * 0.5f), new Vector2(cs, cs), color);
        CreateBar(spawnedRing.transform, "BR",
            new Vector2(size.x * 0.5f, -size.y * 0.5f), new Vector2(cs, cs), color);
    }

    void CreateBar(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    /// <summary>Tạo lớp phủ tối phía sau ring để làm nổi bật icon</summary>
    void CreateDim(Color color)
    {
        spawnedDim = new GameObject("HL_Dim", typeof(RectTransform), typeof(Image));
        spawnedDim.transform.SetParent(hudCanvas.transform, false);

        var rt = spawnedDim.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = spawnedDim.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false; // không chặn click
        spawnedDim.transform.SetAsLastSibling();
    }

    /// <summary>Tạo label "🔒 Chưa mở" bên trên icon locked</summary>
    void CreateLockLabel(RectTransform target)
    {
        spawnedLockText = new GameObject("HL_Lock", typeof(RectTransform), typeof(Image));
        spawnedLockText.transform.SetParent(hudCanvas.transform, false);

        var rt = spawnedLockText.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(80f, 24f);
        rt.anchoredPosition = GetCanvasPos(target) + new Vector2(0, GetSize(target).y * 0.5f + 8f);

        var img = spawnedLockText.GetComponent<Image>();
        img.color = new Color(0.8f, 0.1f, 0.1f, 0.9f);
        img.raycastTarget = false;

        // Text con
        var textGO = new GameObject("LockText", typeof(RectTransform));
        textGO.transform.SetParent(spawnedLockText.transform, false);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        // Dùng Text cũ (không cần TMP để tránh phụ thuộc)
        var text = textGO.AddComponent<Text>();
        text.text = "🔒 Chưa mở";
        text.fontSize = 11;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.raycastTarget = false;

        spawnedLockText.transform.SetAsLastSibling();
    }

    void SpawnOneRing(RectTransform target, Color color)
    {
        if (target == null) return;

        // FIX: cùng lý do như CreateRing() — ép rebuild layout trước khi đọc vị trí.
        Canvas.ForceUpdateCanvases();

        Vector2 pos = GetCanvasPos(target);
        Vector2 size = GetSize(target) + Vector2.one * 20f;

        var container = new GameObject("HL_Ring_Multi", typeof(RectTransform));
        container.transform.SetParent(hudCanvas.transform, false);
        var rt = container.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        container.transform.SetAsLastSibling();

        float thickness = 3f;
        CreateBar(container.transform, "Top",
            new Vector2(0, size.y * 0.5f - thickness * 0.5f),
            new Vector2(size.x, thickness), color);
        CreateBar(container.transform, "Bot",
            new Vector2(0, -size.y * 0.5f + thickness * 0.5f),
            new Vector2(size.x, thickness), color);
        CreateBar(container.transform, "Left",
            new Vector2(-size.x * 0.5f + thickness * 0.5f, 0),
            new Vector2(thickness, size.y), color);
        CreateBar(container.transform, "Right",
            new Vector2(size.x * 0.5f - thickness * 0.5f, 0),
            new Vector2(thickness, size.y), color);

        spawnedRings.Add(container);
    }

    // ══════════════════════════════════════════════════════
    //  COORDINATE HELPERS
    // ══════════════════════════════════════════════════════
    Vector2 GetCanvasPos(RectTransform source)
    {
        Vector3[] corners = new Vector3[4];
        source.GetWorldCorners(corners);
        Vector3 center = (corners[0] + corners[2]) * 0.5f;

        Camera cam = hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                     ? null : hudCanvas.worldCamera;

        Vector2 screenPt = RectTransformUtility.WorldToScreenPoint(cam, center);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT, screenPt, cam, out Vector2 localPt);
        return localPt;
    }

    Vector2 GetSize(RectTransform source)
    {
        Vector3[] corners = new Vector3[4];
        source.GetWorldCorners(corners);

        Camera cam = hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                     ? null : hudCanvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT,
            RectTransformUtility.WorldToScreenPoint(cam, corners[0]),
            cam, out Vector2 bl);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT,
            RectTransformUtility.WorldToScreenPoint(cam, corners[2]),
            cam, out Vector2 tr);

        return new Vector2(Mathf.Abs(tr.x - bl.x), Mathf.Abs(tr.y - bl.y));
    }

    // ══════════════════════════════════════════════════════
    //  COROUTINES
    // ══════════════════════════════════════════════════════
    IEnumerator PulseRing()
    {
        if (ringRT == null) yield break;
        Vector2 baseSize = ringRT.sizeDelta;
        while (ringRT != null)
        {
            float t = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            ringRT.sizeDelta = baseSize + Vector2.one * Mathf.Lerp(0f, pulseExtra, t);

            // Pulse màu alpha
            var bars = ringRT.GetComponentsInChildren<Image>();
            foreach (var bar in bars)
            {
                Color c = bar.color;
                c.a = Mathf.Lerp(0.6f, 1f, t);
                bar.color = c;
            }
            yield return null;
        }
    }

    IEnumerator RedPulseRing()
    {
        if (ringRT == null) yield break;
        Vector2 baseSize = ringRT.sizeDelta;
        while (ringRT != null)
        {
            float t = Mathf.PingPong(Time.time * pulseSpeed * 1.6f, 1f);
            ringRT.sizeDelta = baseSize + Vector2.one * Mathf.Lerp(0f, pulseExtra + 4f, t);

            Color pulseColor = Color.Lerp(
                new Color(1f, 0.15f, 0.15f, 0.9f),
                new Color(1f, 0.65f, 0.1f, 1.0f), t);

            var bars = ringRT.GetComponentsInChildren<Image>();
            foreach (var bar in bars) bar.color = pulseColor;

            yield return null;
        }
    }

    IEnumerator PulseMultipleRings()
    {
        var rts = new List<RectTransform>();
        var bases = new List<Vector2>();
        foreach (var r in spawnedRings)
        {
            if (r == null) continue;
            var rt2 = r.GetComponent<RectTransform>();
            rts.Add(rt2);
            bases.Add(rt2.sizeDelta);
        }
        while (spawnedRings.Count > 0)
        {
            float t = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            for (int i = 0; i < rts.Count; i++)
                if (rts[i] != null)
                    rts[i].sizeDelta = bases[i]
                        + Vector2.one * Mathf.Lerp(0f, pulseExtra, t);
            yield return null;
        }
    }



    /// <summary>Trả về RectTransform icon theo tên field</summary>
    public RectTransform GetIconRT(string name)
    {
        switch (name.ToLower())
        {
            case "woodicon":
            case "wood": return woodIconRT;
            case "stoneicon":
            case "stone": return stoneIconRT;
            case "foodicon":
            case "food":
            case "wheat": return foodIconRT;
            case "worker":
            case "workericon": return workerIconRT;
            case "build":
            case "buildbutton": return buildButtonRT;
            case "daytimer":
            case "day": return dayTimerRT;
            case "enemy":
            case "enemycounter": return enemyCounterRT;
            default:
                Debug.LogWarning($"[HIGHLIGHT] GetIconRT: không nhận ra '{name}'");
                return null;
        }
    }

    /// <summary>Flash xanh lá báo click thành công</summary>
    public void HighlightSuccess(GameObject target)
    {
        ClearAll();
        if (target == null) return;
        var rt = target.GetComponent<RectTransform>();
        if (rt == null) return;
        CreateRing(rt, new Color(0.2f, 0.9f, 0.2f)); // xanh lá
    }
}