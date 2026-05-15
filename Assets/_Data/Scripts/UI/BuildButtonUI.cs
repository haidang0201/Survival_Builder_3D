using UnityEngine;
using UnityEngine.EventSystems;

public class BuildButtonUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Hover")]
    public float hoverScale = 1.1f;

    [Header("Lock")]
    public GameObject lockOverlay;

    private Vector3 defaultScale;

    void Start()
    {
        defaultScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = defaultScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = defaultScale;
    }

    public void SetLocked(bool value)
    {
        if (lockOverlay != null)
            lockOverlay.SetActive(value);
    }
}