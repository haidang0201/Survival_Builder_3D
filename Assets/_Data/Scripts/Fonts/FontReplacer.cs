#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;

public class FontReplacer : EditorWindow
{
    [SerializeField] TMP_FontAsset newFont;
    [SerializeField] Color defaultColor = new Color(0.91f, 0.835f, 0.627f, 1f); // #E8D5A0

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

    void ReplaceAllInScene()
    {
        if (newFont == null) { Debug.LogError("Chưa chọn font!"); return; }

        var allTMP = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
        int count = 0;

        foreach (var tmp in allTMP)
        {
            Undo.RecordObject(tmp, "Replace Font");
            tmp.font = newFont;
            // Chỉ đổi màu nếu đang dùng màu trắng mặc định
            if (tmp.color == Color.white)
                tmp.color = defaultColor;
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
                tmp.font = newFont;
                if (tmp.color == Color.white)
                    tmp.color = defaultColor;
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