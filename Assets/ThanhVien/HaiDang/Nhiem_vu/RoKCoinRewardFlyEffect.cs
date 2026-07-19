using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RoKCoinRewardFlyEffect : MonoBehaviour
{
    [Header("CANVAS")]
    public Canvas targetCanvas;
    public RectTransform flyRoot;

    [Header("VISUAL")]
    public Sprite coinSprite;
    public Vector2 coinSize = new Vector2(38, 38);
    public int maxFlyingCoins = 18;
    public float spawnRadius = 65f;
    public float arcHeight = 110f;
    public float flyDuration = 0.75f;
    public float spawnInterval = 0.035f;

    [Header("SORTING (đảm bảo coin luôn bay TRÊN mọi UI khác)")]
    public bool forceOnTop = true;
    public int overrideSortingOrder = 32767;

    [Header("SOUND")]
    public AudioSource audioSource;
    public AudioClip coinSfx;
    public bool playSfxAtStart = true;
    public bool playSfxOnEachCoinArrive = true;

    void Awake()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>();

        if (targetCanvas == null)
            targetCanvas = FindObjectOfType<Canvas>();

        if (flyRoot == null && targetCanvas != null)
            flyRoot = targetCanvas.GetComponent<RectTransform>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void PlayGoldFly(
        RectTransform from,
        RectTransform to,
        Sprite overrideCoinSprite,
        int amount,
        Action onArrive
    )
    {
        StartCoroutine(PlayGoldFlyRoutine(from, to, overrideCoinSprite, amount, onArrive));
    }

    IEnumerator PlayGoldFlyRoutine(
        RectTransform from,
        RectTransform to,
        Sprite overrideCoinSprite,
        int amount,
        Action onArrive
    )
    {
        if (flyRoot == null || to == null)
        {
            onArrive?.Invoke();
            yield break;
        }

        Sprite usedSprite = overrideCoinSprite != null ? overrideCoinSprite : coinSprite;

        Vector2 startPos = from != null ? GetLocalPoint(from) : Vector2.zero;
        Vector2 endPos = GetLocalPoint(to);

        int coinCount = Mathf.Clamp(
            Mathf.CeilToInt(Mathf.Log10(Mathf.Max(1, amount) + 1) * 6f),
            8,
            maxFlyingCoins
        );

        int arrived = 0;

        if (playSfxAtStart)
            PlayCoinSfx(1f);

        for (int i = 0; i < coinCount; i++)
        {
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * spawnRadius;
            StartCoroutine(FlyOneCoin(
                usedSprite,
                startPos + randomOffset,
                endPos,
                i * 0.015f,
                () => arrived++
            ));

            yield return new WaitForSeconds(spawnInterval);
        }

        while (arrived < coinCount)
            yield return null;

        onArrive?.Invoke();
    }

    IEnumerator FlyOneCoin(
        Sprite sprite,
        Vector2 start,
        Vector2 end,
        float delay,
        Action onDone
    )
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        GameObject go = new GameObject(
            "FlyingGoldCoin",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(Image)
        );
        go.transform.SetParent(flyRoot, false);

        // Đảm bảo coin luôn nằm trên cùng trong cùng 1 Canvas
        go.transform.SetAsLastSibling();

        // Đảm bảo coin luôn vẽ TRÊN mọi Canvas khác (kể cả bảng nhiệm vụ
        // nằm trên 1 Canvas/Panel riêng có sorting order khác nhau).
        if (forceOnTop)
        {
            Canvas coinCanvas = go.AddComponent<Canvas>();
            coinCanvas.overrideSorting = true;
            coinCanvas.sortingOrder = overrideSortingOrder;

            // Cần GraphicRaycaster đi kèm Canvas override để UI không bị lỗi raycast,
            // nhưng coin không nhận raycast nên tắt luôn cho nhẹ.
            GraphicRaycaster gr = go.AddComponent<GraphicRaycaster>();
            gr.enabled = false;
        }

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = coinSize;
        rt.anchoredPosition = start;

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = sprite != null ? Color.white : new Color32(255, 215, 60, 255);
        img.preserveAspect = true;
        img.raycastTarget = false;

        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        cg.alpha = 1f;

        float t = 0f;
        float spin = UnityEngine.Random.Range(-180f, 180f);

        while (t < 1f)
        {
            t += Time.deltaTime / flyDuration;
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            Vector2 p = Vector2.Lerp(start, end, smooth);
            p.y += Mathf.Sin(smooth * Mathf.PI) * arcHeight;

            rt.anchoredPosition = p;
            rt.localScale = Vector3.Lerp(Vector3.one * 1.15f, Vector3.one * 0.65f, smooth);
            rt.localRotation = Quaternion.Euler(0f, 0f, spin * smooth);

            cg.alpha = Mathf.Lerp(1f, 0.85f, smooth);

            yield return null;
        }

        if (playSfxOnEachCoinArrive)
            PlayCoinSfx(UnityEngine.Random.Range(0.95f, 1.08f));

        Destroy(go);

        onDone?.Invoke();
    }


    Vector2 GetLocalPoint(RectTransform rect)
    {
        Camera cam = null;

        if (targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = targetCanvas.worldCamera;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, rect.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            flyRoot,
            screenPoint,
            cam,
            out Vector2 localPoint
        );

        return localPoint;
    }

    void PlayCoinSfx(float pitch)
    {
        if (audioSource == null || coinSfx == null)
            return;

        audioSource.pitch = pitch;
        audioSource.PlayOneShot(coinSfx);
    }
    public void PlayResourceFly(

    RectTransform from,
    RectTransform to,
    Sprite resourceSprite,
    int amount,
    AudioClip overrideSfx,
    System.Action onArrive
)
    {
        AudioClip oldClip = coinSfx;

        if (overrideSfx != null)
            coinSfx = overrideSfx;

        PlayGoldFly(
            from,
            to,
            resourceSprite,
            amount,
            () =>
            {
                coinSfx = oldClip;
                onArrive?.Invoke();
            }
        );
    }

}