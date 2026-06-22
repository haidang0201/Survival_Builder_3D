#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;

public class VietFontBaker
{
    [MenuItem("Tools/Bake Tiếng Việt vào Font")]
    static void Bake()
    {
        // Tìm tất cả TMP_FontAsset trong project
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");

        string vietChars =
            " abcdefghijklmnopqrstuvwxyz" +
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
            "0123456789.,!?:;'\"()-" +
            "àáâãèéêìíòóôõùúýăđơưạảấầẩẫậắằẳẵặẹẻẽếềểễệỉịọỏốồổỗộớờởỡợụủứừửữựỳỷỹỵ" +
            "ÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚÝĂĐƠƯẠẢẤẦẨẪẬẮẰẲẴẶẸẺẼẾỀỂỄỆỈỊỌỎỐỒỔỖỘỚỜỞỠỢỤỦỨỪỬỮỰỲỶỸỴ";

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font == null) continue;

            foreach (char c in vietChars)
                font.HasCharacter(c, true, true);

            EditorUtility.SetDirty(font);
            Debug.Log($"✅ Baked: {font.name}");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("✅ Hoàn tất bake tiếng Việt cho tất cả font!");
    }
}
#endif