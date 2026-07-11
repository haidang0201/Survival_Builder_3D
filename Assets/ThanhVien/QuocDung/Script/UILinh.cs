using UnityEngine;
using TMPro; // Sử dụng thư viện TextMesh Pro
using System.IO; // Sử dụng thư viện System.IO để làm việc với File
using System; // Sử dụng DateTime
using System.Collections.Generic; // Sử dụng List

public class UILinh : MonoBehaviour
{
    [Header("UI Reference")]
    public TMP_Text textCount; // Kéo thả component TextMeshPro (TMP) vào đây để hiển thị

    [Header("Settings")]
    public string soldierTag = "Soldier"; // Tag của lính cần đếm

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
        int count = CountSoldiers();

        // Cập nhật số lượng lên UI Text (nếu đã gán component)
        if (textCount != null)
        {
            textCount.text = "" + count;
        }

        // Tự động lưu toàn bộ dữ liệu game khi số lượng lính thực tế thay đổi
        if (count != lastCount)
        {
            lastCount = count;
            SaveGame();
        }

        // Nhấn phím S để chủ động lưu dữ liệu (Dành cho việc test)
        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("[UILinh] [Phím S] Đang tiến hành lưu thủ công trạng thái công trình và số lượng lính...");
            SaveGame();
        }

        // Nhấn phím L để chủ động tải dữ liệu (Dành cho việc test)
        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("[UILinh] [Phím L] Đang tiến hành tải thủ công trạng thái công trình và số lượng lính...");
            LoadGame();
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

    /// <summary>
    /// Lưu trạng thái công trình (Level) và số lượng lính của từng công trình xuống JSON
    /// </summary>
    public void SaveGame()
    {
        try
        {
            GameSaveData saveData = new GameSaveData();
            saveData.lastSavedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 1. Quét tất cả các UpgradeableBuilding trong Scene
            UpgradeableBuilding[] buildings = FindObjectsOfType<UpgradeableBuilding>();
            foreach (UpgradeableBuilding building in buildings)
            {
                BuildingSaveEntry entry = new BuildingSaveEntry();
                entry.buildingName = building.gameObject.name;
                entry.level = building.CurrentLevel;

                // Lấy đúng spawner đang hoạt động của công trình đó (trên model cấp độ tương ứng hoặc trên chính nó)
                SpawnSoldier spawner = building.GetComponentInChildren<SpawnSoldier>();
                if (spawner != null)
                {
                    entry.soldierCount = spawner.GetActiveSoldiersCount();
                }
                else
                {
                    entry.soldierCount = 0;
                }

                saveData.buildings.Add(entry);
            }

            // 2. Đếm tổng số lính hiện tại trên map
            saveData.totalSoldierCount = CountSoldiers();

            // 3. Ghi dữ liệu xuống file JSON
            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(savePath, json);
            Debug.Log($"[UILinh] Đã lưu dữ liệu trò chơi thành công vào JSON: {savePath}\nNội dung: {json}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[UILinh] Lỗi khi lưu dữ liệu game: {e.Message}");
        }
    }

    /// <summary>
    /// Tải trạng thái công trình (Level) và số lượng lính từ file JSON
    /// </summary>
    public void LoadGame()
    {
        try
        {
            if (File.Exists(savePath))
            {
                string json = File.ReadAllText(savePath);
                GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);

                // 1. Tìm tất cả các UpgradeableBuilding trong scene hiện tại
                UpgradeableBuilding[] sceneBuildings = FindObjectsOfType<UpgradeableBuilding>();

                // 2. Khôi phục trạng thái cho từng công trình
                foreach (BuildingSaveEntry entry in saveData.buildings)
                {
                    UpgradeableBuilding building = Array.Find(sceneBuildings, b => b.gameObject.name == entry.buildingName);
                    if (building != null)
                    {
                        // Khôi phục cấp độ của công trình (Cập nhật visual model & ngắt coroutine xây ban đầu)
                        building.LoadLevel(entry.level);

                        // Tìm spawner sau khi model cấp độ mới đã được bật lên hoạt động
                        SpawnSoldier spawner = building.GetComponentInChildren<SpawnSoldier>();
                        if (spawner != null)
                        {
                            // Spawn lại số lượng lính đã lưu
                            spawner.LoadAndSpawnSoldiers(entry.soldierCount, entry.level);
                        }
                    }
                }

                // Cập nhật lại biến tracking và UI hiển thị
                lastCount = saveData.totalSoldierCount;
                if (textCount != null)
                {
                    textCount.text = "" + lastCount;
                }

                Debug.Log($"[UILinh] Đã tải dữ liệu trò chơi thành công từ JSON: {savePath}\nTổng số lượng lính tải lại: {saveData.totalSoldierCount}");
            }
            else
            {
                Debug.LogWarning($"[UILinh] Không tìm thấy dữ liệu đã lưu tại: {savePath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[UILinh] Lỗi khi tải file JSON: {e.Message}");
        }
    }
}
