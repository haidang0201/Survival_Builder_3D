using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Gắn tạm vào icon HUD lúc tutorial cần detect click.
/// Tự xoá sau khi được click 1 lần — không để lại gì trên HUD gốc.
/// </summary>
public class TutorialClickDetector : MonoBehaviour, IPointerClickHandler
{
    private System.Action onClicked;

    // ══════════════════════════════════════════════════════
    // PUBLIC API — gọi từ TutorialManager.WaitForHUDClick()
    // ══════════════════════════════════════════════════════
    public static TutorialClickDetector Attach(
        GameObject target, System.Action callback)
    {
        Debug.Log($"<color=white>[DETECTOR] Attach → {target?.name}</color>");

        if (target == null)
        {
            Debug.LogError("[DETECTOR] ✗ target NULL — không thể gắn detector!");
            return null;
        }

        // ── Kiểm tra Graphic (Image) để nhận click ──────────────────────
        var graphic = target.GetComponent<Graphic>();
        if (graphic == null)
        {
            Debug.LogError(
                $"[DETECTOR] ✗ {target.name} không có Graphic/Image component!\n" +
                "→ Thêm Image component vào icon này, Raycast Target = true.");
        }
        else if (!graphic.raycastTarget)
        {
            Debug.LogError(
                $"[DETECTOR] ✗ {target.name} có Image nhưng Raycast Target = false!\n" +
                "→ Vào Inspector của icon, tick Raycast Target = true.");
        }
        else
        {
            Debug.Log($"<color=white>[DETECTOR] ✓ {target.name} " +
                      $"có Graphic, raycastTarget=true</color>");
        }

        // ── Kiểm tra Canvas cha ──────────────────────────────────────────
        var canvas = target.GetComponentInParent<Canvas>();
        if (canvas == null)
            Debug.LogError(
                $"[DETECTOR] ✗ {target.name} không thuộc Canvas nào!\n" +
                "→ Đảm bảo icon nằm trong Canvas HUD.");
        else
            Debug.Log($"<color=white>[DETECTOR] Canvas cha: {canvas.name} " +
                      $"(Sort Order {canvas.sortingOrder})</color>");

        // ── Kiểm tra GraphicRaycaster trên Canvas cha ───────────────────
        var raycaster = canvas?.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
            Debug.LogError(
                $"[DETECTOR] ✗ Canvas '{canvas?.name}' không có GraphicRaycaster!\n" +
                "→ Thêm GraphicRaycaster component vào Canvas HUD.");
        else
            Debug.Log($"<color=white>[DETECTOR] ✓ GraphicRaycaster OK " +
                      $"trên {canvas?.name}</color>");

        // ── Gắn detector ─────────────────────────────────────────────────
        // Kiểm tra đã có detector chưa để tránh gắn 2 lần
        var existing = target.GetComponent<TutorialClickDetector>();
        if (existing != null)
        {
            Debug.LogWarning($"[DETECTOR] {target.name} đã có detector, " +
                             "xoá cái cũ trước khi gắn mới.");
            Destroy(existing);
        }

        var detector = target.AddComponent<TutorialClickDetector>();
        detector.onClicked = callback;

        Debug.Log($"<color=white>[DETECTOR] ✓ Đã gắn thành công " +
                  $"vào {target.name}</color>");
        return detector;
    }

    // ══════════════════════════════════════════════════════
    // CLICK HANDLER
    // ══════════════════════════════════════════════════════
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"<color=lime>[DETECTOR] ✓ OnPointerClick fired " +
                  $"→ {gameObject.name} " +
                  $"(button: {eventData.button})</color>");

        onClicked?.Invoke();

        // Tự xoá — không để lại component trên HUD gốc
        Destroy(this);
        Debug.Log($"<color=lime>[DETECTOR] ✓ Detector đã tự xoá " +
                  $"khỏi {gameObject.name}</color>");
    }

    // ══════════════════════════════════════════════════════
    // SAFETY — tự dọn nếu bị Destroy từ bên ngoài
    // ══════════════════════════════════════════════════════
    void OnDestroy()
    {
        onClicked = null;
        Debug.Log($"<color=white>[DETECTOR] OnDestroy → {gameObject?.name}</color>");
    }
}