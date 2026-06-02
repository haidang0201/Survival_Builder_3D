using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Script này gắn vào prefab hoặc quản lý camera click.
/// Khi player click prefab có MyUpgradeableBuilding, sẽ gọi module upgrade.
/// </summary>
public class UpgradeSelector : MonoBehaviour
{
    public BuildingType buildingType; // Nhớ chọn loại nhà tương ứng ngoài Inspector cho từng Prefab nhà nhé!
    
    [Header("Module Upgrade")]
    public UIManager upgradeModule; 

    [Header("Layer của Building")]
    public LayerMask buildingLayerMask;

    [Header("Raycast")]
    public float maxRayDistance = 100f;

    void Update()
    {
        // Click chuột trái để chọn nhà
        if (Input.GetMouseButtonDown(0))
        {
            TrySelectBuilding();
        }
    }

    private void TrySelectBuilding()
    {
        // Tránh click xuyên qua UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // NẾU BUILDING SYSTEM ĐANG TRONG CHẾ ĐỘ DI CHUYỂN HOẶC ĐẶT NHÀ -> KHÔNG CHO CLICK CHỌN NHÀ KHÁC
        // (Bạn có thể map với biến kiểm tra trạng thái bận của BuildingSystem)
        // if (BuildingSystem.Ins.IsBusy) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, buildingLayerMask))
        {
            UpgradeableBuilding building = hit.collider.GetComponentInParent<UpgradeableBuilding>();
            if (building != null && upgradeModule != null)
            {
                // Chỉ mở panel nếu nhà đó đang hoạt động (không bị tạm ẩn để di chuyển)
                if (building.gameObject.activeSelf)
                {
                    upgradeModule.ShowUpgradePanel(building);
                    return;
                }
            }
        }
    }
}