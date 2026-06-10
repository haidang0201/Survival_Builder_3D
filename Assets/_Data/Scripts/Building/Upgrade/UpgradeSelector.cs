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

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit = default;
        bool hasHit = false;

        // 1. Thử quét với LayerMask cấu hình sẵn trong Inspector
        if (buildingLayerMask.value != 0)
        {
            hasHit = Physics.Raycast(ray, out hit, maxRayDistance, buildingLayerMask);
        }

        // 2. Dự phòng: Nếu không trúng, quét toàn bộ các Layer trong Scene
        if (!hasHit)
        {
            hasHit = Physics.Raycast(ray, out hit, maxRayDistance);
        }

        if (hasHit)
        {
            UpgradeableBuilding building = hit.collider.GetComponentInParent<UpgradeableBuilding>();
            if (building != null)
            {
                // Luôn chọn building cho debug screen button
                building.SelectThisBuilding();

                UIManager manager = upgradeModule != null ? upgradeModule : UIManager.Ins;
                if (manager != null && building.gameObject.activeSelf)
                {
                    manager.ShowUpgradePanel(building);
                }
                return;
            }
        }
    }
}