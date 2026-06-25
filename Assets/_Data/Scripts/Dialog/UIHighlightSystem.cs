using UnityEngine;
using UnityEngine.UI;

public class UIHighlightSystem : MonoBehaviour
{
    public static UIHighlightSystem Instance;

    [Header("HUD CANVAS")]
    public Canvas hudCanvas;

    [Header("SPRITES")]
    public Sprite circleSprite;

    // 💥 NEW FEILDS (YOU ASKED)
    [Header("HIGHLIGHT SIZE")]
    public float circleSize = 140f;          // 🔥 chỉnh kích thước vòng

    [Header("PULSE / MOVEMENT FX")]
    public float scaleStrength = 0.1f;       // 🔥 độ phồng to nhỏ
    public float moveStrength = 6f;          // 🔥 độ rung lên xuống mạnh
    public float moveSpeed = 3f;            // 🔥 tốc độ rung

    GameObject dimGO;
    GameObject circleGO;

    RectTransform circleRT;
    RectTransform canvasRT;
    RectTransform currentTarget;

    Vector3 baseScale;
    Vector2 basePos;

    void Awake()
    {
        Instance = this;
        canvasRT = hudCanvas.transform as RectTransform;
    }

    void Update()
    {
        if (circleRT == null) return;

        // 💥 SCALE PULSE
        float scale = 1f + Mathf.Sin(Time.time * 2f) * scaleStrength;
        circleRT.localScale = baseScale * scale;

        // 💥 STRONG FLOAT UP/DOWN
        float yOffset = Mathf.Sin(Time.time * moveSpeed) * moveStrength;
        circleRT.anchoredPosition = basePos + new Vector2(0, yOffset);
    }

    void LateUpdate()
    {
        if (currentTarget == null) return;

        Vector2 pos = GetPos(currentTarget);

        if (circleRT != null)
        {
            basePos = pos;
            circleRT.anchoredPosition = pos;
        }
    }

    // ================= MAIN =================

    public void Highlight(RectTransform target)
    {
        ClearAll();
        if (target == null) return;

        currentTarget = target;

        CreateDim();
        CreateCircle(target);
    }

    // ================= DIM =================

    void CreateDim()
    {
        dimGO = new GameObject("DIM", typeof(RectTransform), typeof(Image));
        dimGO.transform.SetParent(hudCanvas.transform, false);

        var img = dimGO.GetComponent<Image>();
        img.color = new Color(0, 0, 0, 0.6f);
        img.raycastTarget = false;

        RectTransform rt = dimGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
    }

    // ================= CIRCLE =================

    void CreateCircle(RectTransform target)
    {
        circleGO = new GameObject("CIRCLE", typeof(RectTransform), typeof(Image));
        circleGO.transform.SetParent(hudCanvas.transform, false);

        circleRT = circleGO.GetComponent<RectTransform>();

        var img = circleGO.GetComponent<Image>();
        img.sprite = circleSprite;
        img.color = Color.white;
        img.raycastTarget = false;

        circleRT.anchorMin = circleRT.anchorMax = new Vector2(0.5f, 0.5f);

        // 💥 USE FEILD SIZE
        circleRT.sizeDelta = new Vector2(circleSize, circleSize);

        baseScale = circleRT.localScale;
    }

    // ================= POSITION =================

    Vector2 GetPos(RectTransform target)
    {
        Vector3[] c = new Vector3[4];
        target.GetWorldCorners(c);

        Vector3 center = (c[0] + c[2]) * 0.5f;

        Vector2 screen = RectTransformUtility.WorldToScreenPoint(
            hudCanvas.worldCamera,
            center
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT,
            screen,
            hudCanvas.worldCamera,
            out Vector2 pos
        );

        return pos;
    }

    // ================= CLEAR =================

    public void ClearAll()
    {
        currentTarget = null;

        if (dimGO) Destroy(dimGO);
        if (circleGO) Destroy(circleGO);
    }
}