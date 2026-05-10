using System.IO;
using UnityEngine;

public static class FileIO
{
    // Lưu dữ liệu vào file
    public static void SaveToFile(string json, string fileName)
    {
        try
        {
            // Lấy đường dẫn đến thư mục PersistentDataPath để lưu tệp
            string path = Path.Combine(Application.persistentDataPath, fileName);

            // Ghi dữ liệu JSON vào tệp
            File.WriteAllText(path, json);

            Debug.Log($"Saved data to {path}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error saving file: {ex.Message}");  // Xử lý lỗi nếu có
        }
    }

    // Đọc dữ liệu từ file
    public static string LoadFromFile(string fileName)
    {
        try
        {
            string path = Path.Combine(Application.persistentDataPath, fileName);

            // Kiểm tra nếu file tồn tại
            if (!File.Exists(path))
            {
                //                Debug.LogWarning($"File not found: {path}");
                return string.Empty;
            }

            // Đọc dữ liệu từ tệp và trả về
            return File.ReadAllText(path);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error reading file: {ex.Message}");  // Xử lý lỗi nếu có
            return string.Empty;
        }
    }
    // Thêm vào FileIO.cs
    public static bool Delete(string fileName)
    {
        try
        {
            string path = Path.Combine(Application.persistentDataPath, fileName);

            if (!File.Exists(path))
            {
                Debug.LogWarning($"[FileIO] File không tồn tại: {path}");
                return false;
            }

            File.Delete(path);
            Debug.Log($"[FileIO] Đã xóa file: {path}");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FileIO] Lỗi xóa file: {ex.Message}");
            return false;
        }
    }
}