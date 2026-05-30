using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Script này gắn vào prefab hoặc quản lý camera click.
/// Khi player click prefab có MyUpgradeableBuilding, sẽ gọi module upgrade.
/// </summary>
public class UpgradeSelector : MonoBehaviour
{
    [Header("Module Upgrade")]
    public BuildingUpgradeModule upgradeModule; // gán trong inspector

    [Header("Layer của Building")]
    public LayerMask buildingLayerMask;

    [Header("Raycast")]
    public float maxRayDistance = 100f;

    void Update()
    {
        // Click chuột trái
        if (Input.GetMouseButtonDown(0))
        {
            TrySelectBuilding();
        }
    }

    private void TrySelectBuilding()
    {
        // Tránh click UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, buildingLayerMask))
        {
            // Kiểm tra prefab có MyUpgradeableBuilding
            UpgradeableBuilding building = hit.collider.GetComponentInParent<UpgradeableBuilding>();
            if (building != null && upgradeModule != null)
            {
                upgradeModule.ShowUpgradePanel(building);
                return;
            }
        }
    }
}