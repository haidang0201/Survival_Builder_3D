using UnityEngine;
using System.IO;

/*
 * FileIO.cs
 * Folder: Scripts/Systems/
 * Người làm: DŨNG
 *
 * Xử lý đọc/ghi file thuần C#
 * KHÔNG kế thừa MonoBehaviour
 * Được gọi nội bộ bởi JsonDataManager
 */

public static class FileIO
{
    // ================= SAVE =================

    public static void SaveToFile(string json, string fileName)
    {
        string path = BuildPath(fileName);

        // ✅ Tạo thư mục nếu chưa tồn tại
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            Debug.Log($"[FileIO] Tạo thư mục: {directory}");
        }

        File.WriteAllText(path, json);
        Debug.Log($"[FileIO] Đã lưu: {path}");
    }

    // ================= LOAD =================

    public static string LoadFromFile(string fileName)
    {
        try
        {
            string path = BuildPath(fileName);

            if (!File.Exists(path))
            {
                Debug.LogWarning($"[FileIO] File không tồn tại: {path}");
                return string.Empty;
            }

            return File.ReadAllText(path);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FileIO] Lỗi đọc file: {ex.Message}");
            return string.Empty;
        }
    }

    // ================= DELETE =================

    public static bool Delete(string fileName)
    {
        try
        {
            string path = BuildPath(fileName);

            if (!File.Exists(path))
            {
                Debug.LogWarning($"[FileIO] File không tồn tại để xóa: {path}");
                return false;
            }

            File.Delete(path);
            Debug.Log($"[FileIO] Đã xóa: {path}");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FileIO] Lỗi xóa file: {ex.Message}");
            return false;
        }
    }

    // ================= UTILS =================

    private static string BuildPath(string fileName)
    {
        return Path.Combine(Application.persistentDataPath, fileName);
    }
}