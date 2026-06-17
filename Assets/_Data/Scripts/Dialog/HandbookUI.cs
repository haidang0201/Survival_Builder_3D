using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HandbookUI : MonoBehaviour
{
    [SerializeField] private Button tab_Build, tab_Upgrade, tab_Defense;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject content_Build;
    [SerializeField] private GameObject content_Upgrade;
    [SerializeField] private GameObject content_Defense;

    // Gọi từ handbookUI.ShowOnce() sau StoryPanel
    public void ShowOnce()
    {
        gameObject.SetActive(true);
        SwitchTab("Build");
    }

    private void Start()
    {
        tab_Build.onClick.AddListener(() => SwitchTab("Build"));
        tab_Upgrade.onClick.AddListener(() => SwitchTab("Upgrade"));
        tab_Defense.onClick.AddListener(() => SwitchTab("Defense"));
        closeButton.onClick.AddListener(OnHandbookClosed);
    }

    private void SwitchTab(string tab)
    {
        content_Build.SetActive(tab == "Build");
        content_Upgrade.SetActive(tab == "Upgrade");
        content_Defense.SetActive(tab == "Defense");
    }

    public void OnHandbookClosed()
    {
        gameObject.SetActive(false);
        SceneManager.LoadScene("Gameplay");
    }
}
