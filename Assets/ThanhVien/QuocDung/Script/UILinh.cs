using UnityEngine;
using TMPro;

public class UILinh : MonoBehaviour
{
    public static UILinh Instance;

    [Header("UI Reference")]
    public TMP_Text textCount;

    [Header("Settings")]
    public string soldierTag = "Soldier";

    public int soldierCount;


    void Awake()
    {
        Instance = this;
    }


    [Header("Save Settings")]
    public string saveFileName = "game_save_data.json"; // Tên file lưu trữ JSON mới chứa toàn bộ dữ liệu game

    private string savePath;
    private int lastCount = -1; // Biến tạm theo dõi số lượng cũ để tránh ghi file liên tục mỗi frame

    // Cấu trúc lưu trữ thông tin của từng công trình
    [System.Serializable]
    public class BuildingSaveEntry
    {
        public string buildingName;
        public int level;
        public int soldierCount;
    }

    // Cấu trúc dữ liệu lưu trữ toàn bộ game để tuần tự hóa sang JSON
    [System.Serializable]
    public class GameSaveData
    {
        public List<BuildingSaveEntry> buildings = new List<BuildingSaveEntry>();
        public int totalSoldierCount;
        public string lastSavedTime;
    }

    void Awake()
    {
        // Đường dẫn file lưu trữ: AppData/LocalLow/DefaultCompany/Survival_Builder_3D (hoặc tương tự tùy setting project)
        savePath = Path.Combine(Application.persistentDataPath, saveFileName);
    }

    void Start()
    {
        // Tải dữ liệu đã lưu từ trước (nếu có) khi bắt đầu game
        LoadGame();
    }

    void Update()
    {
        soldierCount = CountSoldiers();

        if (textCount != null)
            textCount.text = soldierCount.ToString();
    }


    public int GetSoldierCount()
    {
        return soldierCount;
    }


    public int CountSoldiers()
    {
        GameObject[] soldiers = GameObject.FindGameObjectsWithTag(soldierTag);

        return soldiers.Length;
    }
}