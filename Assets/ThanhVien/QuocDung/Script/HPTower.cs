using UnityEngine;
using UnityEngine.UI;

public class HPTower : MonoBehaviour, IDamageable
{
    [Header("Cấu hình máu (HP)")]
    [SerializeField] private float maxHealth = 200f;
    [SerializeField] private GameObject destroyVFXPrefab; // Hiệu ứng khi công trình bị phá hủy (nếu có)

    [Header("Cấu hình UI Máu (HP Bar)")]
    [SerializeField] private float hpBarHeightOffset = 4f;
    [SerializeField] private Vector2 hpBarSize = new Vector2(2f, 0.25f);
    [SerializeField] private Color hpBarColor = Color.red;
    [SerializeField] private bool hideHpBarWhenFull = true;

    public float CurrentHealth { get; set; }
    public float MaxHealth { get; set; }

    private bool isDestroyed = false;

    private Canvas hpCanvas;
    private Image hpFillImage;
    private Transform camTransform;
    private Sprite whiteSprite;

    private void Awake()
    {
        MaxHealth = maxHealth;
        CurrentHealth = MaxHealth;

        // Tạo sprite trắng 1x1 tại runtime để gán cho các Image của Canvas (bắt buộc đối với Image.Type.Filled)
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));

        CreateHPBar();
    }

    private void CreateHPBar()
    {
        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }

        // Tự động quy đổi kích thước sang pixel để hiển thị sắc nét và mượt mà
        Vector2 finalPixelSize = hpBarSize;
        float baseScale = 0.01f;

        if (hpBarSize.x < 10f)
        {
            finalPixelSize = hpBarSize * 100f; // Ví dụ: (2, 0.25) -> (200, 25)
            baseScale = 0.01f;
        }
        else
        {
            baseScale = 2f / hpBarSize.x;
        }

        // Tạo Canvas Game Object
        GameObject canvasObj = new GameObject("HPBar_Canvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = new Vector3(0, hpBarHeightOffset, 0);
        canvasObj.transform.localRotation = Quaternion.identity;

        // Bù trừ tỉ lệ co giãn của cha (transform.lossyScale) để chống méo mó
        Vector3 parentScale = transform.lossyScale;
        float scaleX = parentScale.x > 0.0001f ? (baseScale / parentScale.x) : baseScale;
        float scaleY = parentScale.y > 0.0001f ? (baseScale / parentScale.y) : baseScale;
        float scaleZ = parentScale.z > 0.0001f ? (baseScale / parentScale.z) : baseScale;
        canvasObj.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);

        hpCanvas = canvasObj.AddComponent<Canvas>();
        hpCanvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = finalPixelSize;

        // Tạo Background GameObject
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.sprite = whiteSprite; // Gán sprite trắng
        bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.85f); // Nền xám đen mờ
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Tạo Fill GameObject
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(canvasObj.transform, false);
        hpFillImage = fillObj.AddComponent<Image>();
        hpFillImage.sprite = whiteSprite; // Gán sprite trắng để sử dụng được Type.Filled
        hpFillImage.color = hpBarColor;
        hpFillImage.type = Image.Type.Filled;
        hpFillImage.fillMethod = Image.FillMethod.Horizontal;
        hpFillImage.fillAmount = 1f;

        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        // Ẩn thanh máu lúc đầu nếu được cấu hình ẩn khi đầy máu
        if (hideHpBarWhenFull)
        {
            canvasObj.SetActive(false);
        }
    }

    public void TakeDamage(float amount, Vector3 hitPoint)
    {
        if (isDestroyed) return;

        CurrentHealth -= amount;
        Debug.Log($"[HPTower] {gameObject.name} nhận {amount} sát thương tại {hitPoint}. HP còn lại: {CurrentHealth}/{MaxHealth}");

        // Kích hoạt hiệu ứng rung lắc hoặc va chạm nhẹ ở đây nếu cần thiết
        UpdateHPBar();

        if (CurrentHealth <= 0f)
        {
            CurrentHealth = 0f;
            OnDeath();
        }
    }

    private void UpdateHPBar()
    {
        if (hpFillImage == null) return;

        float ratio = Mathf.Clamp01(CurrentHealth / MaxHealth);
        hpFillImage.fillAmount = ratio;

        if (hpCanvas != null)
        {
            bool shouldShow = ratio > 0f && (!hideHpBarWhenFull || ratio < 1f);
            if (hpCanvas.gameObject.activeSelf != shouldShow)
            {
                hpCanvas.gameObject.SetActive(shouldShow);
            }
        }
    }

    private void LateUpdate()
    {
        if (hpCanvas != null && hpCanvas.gameObject.activeSelf)
        {
            if (camTransform == null && Camera.main != null)
            {
                camTransform = Camera.main.transform;
            }

            if (camTransform != null)
            {
                // Xoay Canvas luôn đối diện với camera
                hpCanvas.transform.rotation = camTransform.rotation;
            }

            // Bù trừ tỉ lệ co giãn của cha nếu cha thay đổi scale để tránh méo mó
            Vector3 parentScale = transform.lossyScale;
            float baseScale = (hpBarSize.x < 10f) ? 0.01f : (2f / hpBarSize.x);
            float scaleX = parentScale.x > 0.0001f ? (baseScale / parentScale.x) : baseScale;
            float scaleY = parentScale.y > 0.0001f ? (baseScale / parentScale.y) : baseScale;
            float scaleZ = parentScale.z > 0.0001f ? (baseScale / parentScale.z) : baseScale;
            hpCanvas.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
        }
    }

    public void OnDeath()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        Debug.Log($"[HPTower] {gameObject.name} đã bị phá hủy hoàn toàn!");

        // Tạo hiệu ứng phá hủy nếu có gán prefab
        if (destroyVFXPrefab != null)
        {
            GameObject vfx = Instantiate(destroyVFXPrefab, transform.position, transform.rotation);
            Destroy(vfx, 2f);
        }

        // Hủy đối tượng công trình
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (whiteSprite != null)
        {
            if (whiteSprite.texture != null)
            {
                Destroy(whiteSprite.texture);
            }
            Destroy(whiteSprite);
        }
    }
}

