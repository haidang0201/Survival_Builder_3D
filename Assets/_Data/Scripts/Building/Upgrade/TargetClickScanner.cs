// using UnityEngine;
// using UnityEngine.EventSystems;

// public class TargetClickScanner : MonoBehaviour
// {
//     [Header("Cấu hình quét chuột 3D")]
//     [SerializeField] private LayerMask buildingLayer;
//     [SerializeField] private BuildingUpgradeUI upgradeUI;

//     void Update()
//     {
//         if (Input.GetMouseButtonDown(0))
//         {
//             Debug.Log("<color=cyan><b>[Bước 1]</b></color> Bạn đã click chuột trái ngoài màn hình.");

//             // Kiểm tra UI có chặn không
//             if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
//             {
//                 Debug.LogWarning("<color=red><b>[LỖI]</b></color> Cú click bị CHẶN bởi một linh kiện UI (Canvas, Panel mờ, Icon...) đang đè phía trước!");
//                 return;
//             }

//             // Kiểm tra Camera Tag
//             if (Camera.main == null)
//             {
//                 Debug.LogError("<color=red><b>[LỖI]</b></color> Không tìm thấy Main Camera! Hãy kiểm tra xem Camera đã được đổi Tag thành 'MainCamera' chưa.");
//                 return;
//             }

//             Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
//             RaycastHit hit;

//             // Bắn tia TỰ DO (Không lọc Layer trước) để xem chuột có chạm trúng cái gì không
//             if (Physics.Raycast(ray, out hit, 1000f))
//             {
//                 GameObject objTrungTia = hit.collider.gameObject;
//                 string layerName = LayerMask.LayerToName(objTrungTia.layer);

//                 Debug.Log($"<color=yellow><b>[Bước 2]</b></color> Tia chuột đã bắn trúng vật thể: <b>'{objTrungTia.name}'</b> | Layer hiện tại của nó là: <b>'{layerName}'</b>");

//                 // Bây giờ mới kiểm tra xem vật thể trúng tia có thuộc Layer được chỉ định không
//                 if (((1 << objTrungTia.layer) & buildingLayer) != 0)
//                 {
//                     Debug.Log("<color=green><b>[Bước 3]</b></color> Vật thể này ĐÚNG thuộc Layer bạn đã chọn trong ô Building Layer.");

//                     UpgradeableBuilding building = objTrungTia.GetComponentInParent<UpgradeableBuilding>();
//                     if (building != null)
//                     {
//                         Debug.Log($"<color=green><b>[Bước 4]</b></color> Đã tìm thấy script UpgradeableBuilding trên nhà: <b>{building.buildingName}</b>");

//                         if (upgradeUI != null)
//                         {
//                             upgradeUI.OpenUI(building);
//                             Debug.Log("<color=lime><b>[THÀNH CÔNG]</b></color> Đã gọi lệnh mở UI nâng cấp thành công!");
//                         }
//                         else
//                         {
//                             Debug.LogError("<color=red><b>[LỖI]</b></color> Ô biến 'Upgrade UI' trên Camera đang bị TRỐNG! Bạn chưa kéo bảng UI của Vũ vào.");
//                         }
//                     }
//                     else
//                     {
//                         Debug.LogWarning("<color=orange><b>[CẢNH BÁO]</b></color> Vật thể có Layer chuẩn nhưng KHÔNG tìm thấy script UpgradeableBuilding trên nó hoặc trên Cha của nó!");
//                     }
//                 }
//                 else
//                 {
//                     Debug.LogWarning($"<color=orange><b>[CẢNH BÁO]</b></color> Tia chuột trúng nhà, nhưng do Layer của vật thể này đang là <b>'{layerName}'</b> chứ không phải Layer bạn chọn trong LayerMask trên Camera!");
//                 }
//             }
//             else
//             {
//                 Debug.LogWarning("<color=orange><b>[Bước 2 Thất Bại]</b></color> Tia chuột bắn vào khoảng không, không trúng bất kỳ Collider 3D nào trên Map!");
//             }
//         }
//     }
// }