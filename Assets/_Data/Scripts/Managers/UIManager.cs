using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    [SerializeField] private GameObject buildMenu;
    [SerializeField] private GameObject warningUI;
    public void ToggleBuildMenu()
    {
        buildMenu.SetActive(!buildMenu.activeSelf);
    }

    // Hiển thị cảnh báo trên giao diện
    public void ShowWarning(string message)
    {
        warningUI.SetActive(true);
    }

    // Ẩn cảnh báo trên giao diện
    public void HideWarning()
    {
        warningUI.SetActive(false);
    }
    public void OnClickHouseButton()
    {
        BuildingSystem.Ins.StartPlacing(BuildingType.House);
    }

    public void OnClickForestHutButton()
    {
        BuildingSystem.Ins.StartPlacing(BuildingType.ForestHut);
    }
    public void OnClickSawmillButton()
    {
        BuildingSystem.Ins.StartPlacing(BuildingType.Sawmill);
    }
    public void OnClickWarehouseButton()
    {
        BuildingSystem.Ins.StartPlacing(BuildingType.Warehouse);
    }
    public void OnClickHouseBuilderButton()
    {
        BuildingSystem.Ins.StartPlacing(BuildingType.House);
    }
}