#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;

public class FontReplacer : EditorWindow
{
    [SerializeField] TMP_FontAsset newFont;

    // Lấy mẫu trực tiếp từ ảnh UI mẫu: chữ nhãn công trình (House, Farm Plot, Sawmill...)
    // trên nền giấy da sáng dùng màu ĐEN THUẦN #000000 -> tương phản tối đa trên nền trắng/giấy da.
    [SerializeField] Color defaultColor = new Color(0f, 0f, 0f, 1f); // #000000

    [SerializeField] bool applyOutline = true;

    // Viền nâu sẫm đo được quanh chữ trắng (tiêu đề "Village Builder", số tài nguyên "1,250"...)
    // trong ảnh mẫu: trung bình RGB (57,46,37) -> #392E25. Dùng chung viền này để đồng bộ
    // phong cách "khắc chữ" trên mọi nền (trắng, gỗ tối, banner màu).
    [SerializeField] Color outlineColor = new Color(0.222f, 0.180f, 0.145f, 0.95f); // #392E25
    [SerializeField, Range(0f, 1f)] float outlineWidth = 0.2f;

    [MenuItem("Tools/Font Replacer")]
    public static void ShowWindow()
    {
        GetWindow<FontReplacer>("Font Replacer");
    }

    void OnGUI()
    {
        GUILayout.Label("Thay Font Toàn Bộ TMP", EditorStyles.boldLabel);
        GUILayout.Space(8);

        newFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
            "Font Asset Mới", newFont, typeof(TMP_FontAsset), false);

        defaultColor = EditorGUILayout.ColorField("Màu chữ mặc định", defaultColor);

        GUILayout.Space(6);
        applyOutline = EditorGUILayout.Toggle("Áp viền tối cho chữ (nổi trên nền trắng)", applyOutline);

        using (new EditorGUI.DisabledScope(!applyOutline))
        {
            outlineColor = EditorGUILayout.ColorField("Màu viền", outlineColor);
            outlineWidth = EditorGUILayout.Slider("Độ dày viền", outlineWidth, 0f, 1f);
        }

        GUILayout.Space(12);

        if (GUILayout.Button("🔄 Thay tất cả TMP trong Scene", GUILayout.Height(36)))
        {
            ReplaceAllInScene();
        }

        GUILayout.Space(6);

        if (GUILayout.Button("📁 Thay tất cả TMP trong Prefabs", GUILayout.Height(36)))
        {
            ReplaceAllInPrefabs();
        }
    }

    void ApplyStyle(TMP_Text tmp)
    {
        tmp.font = newFont;

        // Chỉ đổi màu nếu đang dùng màu trắng mặc định
        if (tmp.color == Color.white)
            tmp.color = defaultColor;

        // Viền nâu sẫm giúp chữ nổi chắc chắn dù rơi vào nền trắng, giấy da, gỗ tối hay banner màu.
        if (applyOutline)
        {
            tmp.outlineColor = outlineColor;
            tmp.outlineWidth = outlineWidth;
        }
    }

    void ReplaceAllInScene()
    {
        if (newFont == null) { Debug.LogError("Chưa chọn font!"); return; }

        var allTMP = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
        int count = 0;

        foreach (var tmp in allTMP)
        {
            Undo.RecordObject(tmp, "Replace Font");
            ApplyStyle(tmp);
            EditorUtility.SetDirty(tmp);
            count++;
        }

        Debug.Log($"✅ Đã thay font cho {count} TMP objects trong Scene.");
    }

    void ReplaceAllInPrefabs()
    {
        if (newFont == null) { Debug.LogError("Chưa chọn font!"); return; }

        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            var tmps = prefab.GetComponentsInChildren<TMP_Text>(true);
            if (tmps.Length == 0) continue;

            bool changed = false;
            foreach (var tmp in tmps)
            {
                ApplyStyle(tmp);
                EditorUtility.SetDirty(tmp);
                changed = true;
                count++;
            }

            if (changed)
                PrefabUtility.SavePrefabAsset(prefab);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"✅ Đã thay font cho {count} TMP objects trong Prefabs.");
    }
}
#endif