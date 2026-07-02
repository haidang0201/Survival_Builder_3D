using UnityEngine;
using UnityEngine.UI;

public class WorldTutorialArrow : MonoBehaviour
{
    [Header("UI")]
    public Canvas tutorialCanvas;
    public RectTransform arrowRect;
    public Image arrowImage;

    [Header("CAMERA")]
    public Camera worldCamera;

    [Header("TARGET")]
    public Transform target;

    [Header("POSITION")]
    public Vector3 worldOffset = new Vector3(0f, 2.5f, 0f);
    public Vector2 screenOffset = new Vector2(0f, 90f);

    [Header("ANIMATION")]
    public float floatAmplitude = 12f;
    public float floatSpeed = 3f;

    [Header("ROTATION")]
    public float arrowRotationZ = 0f;

    private RectTransform canvasRect;
    private bool isShowing;
    private Vector2 basePos;

    void Awake()
    {
        if (tutorialCanvas == null)
            tutorialCanvas = GetComponentInParent<Canvas>();

        if (tutorialCanvas != null)
            canvasRect = tutorialCanvas.transform as RectTransform;

        if (worldCamera == null)
            worldCamera = Camera.main;

        if (arrowRect != null)
            arrowRect.gameObject.SetActive(false);

        if (arrowImage == null && arrowRect != null)
            arrowImage = arrowRect.GetComponent<Image>();

        if (arrowImage != null)
            arrowImage.raycastTarget = false;
    }

    void LateUpdate()
    {
        if (!isShowing || target == null || arrowRect == null || tutorialCanvas == null)
            return;

        UpdateArrowPosition();
        AnimateArrow();
    }

    public void Show(Transform newTarget)
    {
        target = newTarget;
        isShowing = true;

        if (arrowRect != null)
        {
            arrowRect.gameObject.SetActive(true);
            arrowRect.SetAsLastSibling();
        }

        UpdateArrowPosition();
    }

    public void Hide()
    {
        isShowing = false;
        target = null;

        if (arrowRect != null)
            arrowRect.gameObject.SetActive(false);
    }

    void UpdateArrowPosition()
    {
        if (target == null || worldCamera == null || canvasRect == null)
            return;

        Vector3 worldPos = target.position + worldOffset;
        Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPos);

        if (screenPos.z < 0f)
        {
            arrowRect.gameObject.SetActive(false);
            return;
        }

        if (!arrowRect.gameObject.activeSelf)
            arrowRect.gameObject.SetActive(true);

        Camera canvasCam = null;

        if (tutorialCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            canvasCam = tutorialCanvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            canvasCam,
            out Vector2 localPoint
        );

        basePos = localPoint + screenOffset;
        arrowRect.anchoredPosition = basePos;
        arrowRect.localRotation = Quaternion.Euler(0f, 0f, arrowRotationZ);
    }

    void AnimateArrow()
    {
        float y = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        arrowRect.anchoredPosition = basePos + new Vector2(0f, y);
    }
}