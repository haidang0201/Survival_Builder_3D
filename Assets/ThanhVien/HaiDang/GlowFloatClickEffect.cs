using UnityEngine;
using UnityEngine.UI;

public class GlowFloatClickEffect : MonoBehaviour
{
    [Header("FLOAT SETTINGS")]
    public float floatHeight = 3f;
    public float floatSpeed = 2f;

    [Header("ROTATE / SHAKE")]
    public float rotateAmount = 5f;
    public float rotateSpeed = 1.5f;

    [Header("GLOW EFFECT (light scale pulse)")]
    public float glowSpeed = 3f;
    public float glowAmount = 0.08f;

    bool isSelected;

    float baseLocalY;
    Vector3 startScale;

    void Start()
    {
        // Ép Layout Group của cha tính toán xong vị trí ban đầu trước khi lưu base Y
        if (transform.parent != null && transform.parent is RectTransform parentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }

        baseLocalY = transform.localPosition.y;
        startScale = transform.localScale;
    }

    void Update()
    {
        if (isSelected) return;

        // ================= FLOAT =================
        float y = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        // Giữ nguyên Pos X do HorizontalLayoutGroup sắp xếp, chỉ thay đổi Y
        Vector3 curPos = transform.localPosition;
        transform.localPosition = new Vector3(curPos.x, baseLocalY + y, curPos.z);

        // ================= ROTATE =================
        float rot = Mathf.Sin(Time.time * rotateSpeed) * rotateAmount;
        transform.localRotation = Quaternion.Euler(0, 0, rot);

        // ================= GLOW (SCALE PULSE) =================
        float scalePulse = 1f + Mathf.Sin(Time.time * glowSpeed) * glowAmount;
        transform.localScale = startScale * scalePulse;
    }

    void OnMouseDown()
    {
        // 🔥 CLICK → đứng im
        isSelected = true;

        Vector3 curPos = transform.localPosition;
        transform.localPosition = new Vector3(curPos.x, baseLocalY, curPos.z);
        transform.localRotation = Quaternion.identity;
        transform.localScale = startScale;
    }

    // optional: bỏ chọn lại
    public void Deselect()
    {
        isSelected = false;
    }
}