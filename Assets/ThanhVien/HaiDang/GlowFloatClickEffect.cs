using UnityEngine;

public class GlowFloatClickEffect : MonoBehaviour
{
    [Header("FLOAT SETTINGS")]
    public float floatHeight = 0.1f;
    public float floatSpeed = 2f;

    [Header("ROTATE / SHAKE")]
    public float rotateAmount = 5f;
    public float rotateSpeed = 1.5f;

    [Header("GLOW EFFECT (light scale pulse)")]
    public float glowSpeed = 3f;
    public float glowAmount = 0.08f;

    bool isSelected;

    Vector3 startPos;
    Vector3 startScale;

    void Start()
    {
        startPos = transform.localPosition;
        startScale = transform.localScale;
    }

    void Update()
    {
        if (isSelected) return;

        // ================= FLOAT =================
        float y = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.localPosition = startPos + new Vector3(0, y, 0);

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

        transform.localPosition = startPos;
        transform.localRotation = Quaternion.identity;
        transform.localScale = startScale;
    }

    // optional: bỏ chọn lại
    public void Deselect()
    {
        isSelected = false;
    }
}