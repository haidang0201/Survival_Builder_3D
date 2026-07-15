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

    // Trả về full path trong Hierarchy để dễ tìm object bị lỗi (VD: "Canvas/Dropdown/Template/Item").
    string GetHierarchyPath(Transform t)
    {
        if (t == null)
            return "(null)";

        string path = t.name;
        Transform current = t.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    void ReplaceAllInScene()
    {
        if (newFont == null) { Debug.LogError("Chưa chọn font!"); return; }

        var allTMP = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;
        int skipped = 0;

        foreach (var tmp in allTMP)
        {
            if (tmp == null)
                continue;

            try
            {
                Undo.RecordObject(tmp, "Replace Font");
                ApplyStyle(tmp);
                EditorUtility.SetDirty(tmp);
                count++;
            }
            catch (System.Exception ex)
            {
                // Một số object (thường là template ẩn của TMP_Dropdown, hoặc object đang
                // inactive chưa từng OnEnable) chưa được Unity khởi tạo CanvasRenderer/material,
                // gây UnassignedReferenceException khi gán font. Bỏ qua object đó và log cảnh báo
                // thay vì để lỗi dừng ngang cả vòng lặp (đó là lý do trước đây chỉ vài object
                // được đổi trong khi các object còn lại bị bỏ sót không rõ lý do).
                skipped++;
                Debug.LogWarning($"[FontReplacer] Bỏ qua '{GetHierarchyPath(tmp.transform)}' vì lỗi: {ex.Message}");
            }
        }

        Debug.Log($"✅ Đã thay font cho {count} TMP objects trong Scene." +
                  (skipped > 0 ? $" ⚠ Bỏ qua {skipped} object bị lỗi (xem Console để biết chi tiết)." : ""));
    }

    void ReplaceAllInPrefabs()
    {
        if (newFont == null) { Debug.LogError("Chưa chọn font!"); return; }

        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int count = 0;
        int skipped = 0;

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
                if (tmp == null)
                    continue;

                try
                {
                    ApplyStyle(tmp);
                    EditorUtility.SetDirty(tmp);
                    changed = true;
                    count++;
                }
                catch (System.Exception ex)
                {
                    // Xem giải thích ở ReplaceAllInScene(): bỏ qua object lỗi thay vì dừng cả loop.
                    skipped++;
                    Debug.LogWarning($"[FontReplacer] Bỏ qua '{path} -> {GetHierarchyPath(tmp.transform)}' vì lỗi: {ex.Message}");
                }
            }

            if (changed)
                PrefabUtility.SavePrefabAsset(prefab);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"✅ Đã thay font cho {count} TMP objects trong Prefabs." +
                  (skipped > 0 ? $" ⚠ Bỏ qua {skipped} object bị lỗi (xem Console để biết chi tiết)." : ""));
    }
}
#endif