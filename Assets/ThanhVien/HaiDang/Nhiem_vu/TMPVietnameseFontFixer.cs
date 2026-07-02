using UnityEngine;
using TMPro;

public class TMPVietnameseFontFixer : MonoBehaviour
{
    [Header("FONT TIẾNG VIỆT")]
    public TMP_FontAsset vietnameseFont;

    [Header("OPTIONAL MATERIAL")]
    public Material fontMaterialPreset;

    [Header("SETTINGS")]
    public bool includeInactive = true;

    // Bật cái này khi UI nhiệm vụ sinh text bằng code lúc runtime
    public bool autoRefreshWhenTextCreated = true;

    int lastTextCount = -1;

    void Awake()
    {
        ApplyFont();
    }

    void Start()
    {
        ApplyFont();
    }

    void OnEnable()
    {
        ApplyFont();
    }

    void LateUpdate()
    {
        if (!autoRefreshWhenTextCreated) return;

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(includeInactive);

        if (texts.Length != lastTextCount)
        {
            ApplyFont(texts);
            lastTextCount = texts.Length;
        }
    }

    public void ApplyFont()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(includeInactive);
        ApplyFont(texts);
        lastTextCount = texts.Length;
    }

    void ApplyFont(TMP_Text[] texts)
    {
        if (vietnameseFont == null)
        {
            Debug.LogWarning("[TMPVietnameseFontFixer] Chưa gán Vietnamese TMP Font.");
            return;
        }

        foreach (TMP_Text t in texts)
        {
            if (t == null) continue;

            t.font = vietnameseFont;

            if (fontMaterialPreset != null)
                t.fontSharedMaterial = fontMaterialPreset;

            t.richText = true;
            t.ForceMeshUpdate();
        }

        Debug.Log("[TMPVietnameseFontFixer] Đã apply font tiếng Việt cho " + texts.Length + " TMP Text.");
    }
}