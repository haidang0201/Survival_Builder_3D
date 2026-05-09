using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    [SerializeField] private GameObject warningUI;

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
}