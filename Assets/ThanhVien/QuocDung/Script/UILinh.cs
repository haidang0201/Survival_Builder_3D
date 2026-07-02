using UnityEngine;
using TMPro; // Sử dụng thư viện TextMesh Pro

public class UILinh : MonoBehaviour
{
    [Header("UI Reference")]
    public TMP_Text textCount; // Kéo thả component TextMeshPro (TMP) vào đây để hiển thị

    [Header("Settings")]
    public string soldierTag = "Soldier"; // Tag của lính cần đếm

    void Update()
    {
        int count = CountSoldiers();

        // Cập nhật số lượng lên UI Text (nếu đã gán component)
        if (textCount != null)
        {
            textCount.text = "" + count;
        }
    }

    /// <summary>
    /// Đếm số lượng lính có tag Soldier trên Map
    /// </summary>
    public int CountSoldiers()
    {
        GameObject[] soldiers = GameObject.FindGameObjectsWithTag(soldierTag);
        
        // In ra tên từng đối tượng tìm thấy để dễ debug
        foreach (GameObject s in soldiers)
        {
            Debug.Log("Tìm thấy Soldier: " + s.name + " | Path: " + GetGameObjectPath(s), s);
        }
        
        return soldiers.Length;
    }

    // Hàm phụ trợ để lấy đường dẫn đầy đủ của GameObject trong Hierarchy
    private string GetGameObjectPath(GameObject obj)
    {
        string path = "/" + obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = "/" + obj.name + path;
        }
        return path;
    }
}
