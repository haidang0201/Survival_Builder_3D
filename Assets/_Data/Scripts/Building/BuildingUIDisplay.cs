// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;

// public class BuildingUIDisplay : MonoBehaviour
// {
//     [Header("--- KẾT NỐI UI HIERARCHY ---")]
//     public Image iconImage;                 // Kéo Object 'Icon' ở khối Header vào đây
//     public TextMeshProUGUI nameText;        // Kéo Object 'Name' vào đây
//     public TextMeshProUGUI levelText;       // Kéo Object 'Level' vào đây
//     public TextMeshProUGUI descriptionText; // Kéo Object 'Text (TMP)' ở khối Midle vào đây

//     [Header("--- VÙNG CHỨA STATUS (MIDLE -> IMFO) ---")]
//     public Transform statsGridParent;       // Kéo Object 'Imfo' (con của Midle) vào đây
//     public GameObject statRowPrefab;        // Kéo Prefab dòng chữ mẫu (Tên - Con số) vào đây

//     // HÀM CHÍNH: Gọi hàm này truyền BuildingCtrl của nhà được Click vào
//     public void DisplayBuildingInformation(BuildingCtrl building, BuildingData staticData)
//     {
//         if (building == null || statRowPrefab == null || statsGridParent == null) return;

//         // 1. Đổ dữ liệu Header cơ bản
//         nameText.text = building.buildingType.ToString().ToUpper();
//         levelText.text = "CẤP ĐỘ " + staticData.level; // Lấy level từ config hoặc state
//         descriptionText.text = $"Công trình thuộc nhóm {building.buildingType}";

//         // 2. Dọn dẹp các dòng Status cũ trong khung Imfo
//         foreach (Transform child in statsGridParent)
//         {
//             Destroy(child.gameObject);
//         }

//         // 3. XỬ LÝ PHÂN LOẠI CÔNG TRÌNH THEO ĐÚNG BUILDINGTYPE ENUM
//         BuildingType type = building.buildingType;

//         // --- NHÓM 1: NHÀ CHÍNH ---
//         // (Trong enum chưa thấy TownHall, nếu bạn dùng Warehouse làm nhà chính thì check tại đây)
//         if (type == BuildingType.Warehouse) 
//         {
//             CreateStatRow("Máu Hệ Thống:", $"{building.currentHealth}/{building.maxHealth}");
//         }
        
//         // --- NHÓM 2: CÔNG TRÌNH PHÒNG THỦ & QUÂN SỰ ---
//         else if (type == BuildingType.WatchTower || type == BuildingType.ArcherTower || type == BuildingType.Cannon ||
//                  type == BuildingType.BarracksMelee || type == BuildingType.BarracksArcher || type == BuildingType.BarracksSpear)
//         {
//             // Cố gắng lấy component chiến đấu từ tháp phòng thủ để rút chỉ số sát thương
//             TowerCombatCtrl combatCtrl = building.GetComponent<TowerCombatCtrl>();
//             if (combatCtrl != null)
//             {
//                 CreateStatRow("Sát Thương:", combatCtrl.damageDay.ToString());
//                 CreateStatRow("Tốc Độ Bắn:", (1f / combatCtrl.fireRateDay).ToString("F1") + "s / viên");
//             }
//             else
//             {
//                 // Nếu là nhà lính chưa có combatCtrl thì hiện mặc định
//                 CreateStatRow("Sát Thương:", "0");
//                 CreateStatRow("Tốc Độ Bắn:", "0s");
//             }

//             CreateStatRow("Máu Công Trình:", $"{building.currentHealth}/{building.maxHealth}");
//             CreateStatRow("Binh Lính Chứa:", $"{building.currentSoldiers}/{building.maxSoldiers} Binh lính");
//         }
        
//         // --- NHÓM 3: CÔNG TRÌNH DÂN SỰ (Nhà ở, Sản xuất, Lưu trữ còn lại) ---
//         else
//         {
//             // Giả lập hiệu suất dựa trên trạng thái hoạt động isOccupied của bạn
//             float efficiency = building.IsOccupied ? 100f : 0f; 

//             CreateStatRow("Hiệu Suất:", efficiency + "%");
//             CreateStatRow("Máu Công Trình:", $"{building.currentHealth}/{building.maxHealth}");
//             CreateStatRow("Số Công Nhân:", $"{building.currentWorkers}/{building.maxWorkers}");
//         }
//     }

//     // Hàm phụ trợ tạo nhanh dòng chữ UI mẫu
//     private void CreateStatRow(string statName, string statValue)
//     {
//         GameObject newRow = Instantiate(statRowPrefab, statsGridParent);
//         TextMeshProUGUI[] texts = newRow.GetComponentsInChildren<TextMeshProUGUI>();
//         if (texts.Length >= 2)
//         {
//             texts[0].text = statName;
//             texts[1].text = statValue;
//         }
//     }
// }