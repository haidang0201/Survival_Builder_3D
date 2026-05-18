using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class UIManager : Singleton<UIManager>
{
    [SerializeField] private GameObject buildMenu;
    [SerializeField] private GameObject warningUI;
    public void ToggleBuildMenu()
    {
        buildMenu.SetActive(!buildMenu.activeSelf);
    }

    [SerializeField] private GameObject houseSelectionPanel;
    [SerializeField] private GameObject workerStatusPanel;


    //========================PauseMenu============================
    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Scene")]
    [SerializeField] private string gameplaySceneName;

    private bool isPaused = false;

    //==============================================================


    void Start()
    {
        houseSelectionPanel.SetActive(true);
        workerStatusPanel.SetActive(true);
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

    //========================PauseMenu============================
    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (!isPaused)
                PauseGame();
            else
                ResumeGame();
        }
    }

    // ================= PAUSE =================

    void PauseGame()
    {
        isPaused = true;

        pausePanel.SetActive(true);
        settingsPanel.SetActive(false);

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ResumeGame()
    {
        isPaused = false;

        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);

        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ================= BUTTON =================

    // Nút Continue
    public void OnClickContinue()
    {
        ResumeGame();
    }

    // Nút mở Settings
    public void OnClickOpenSettings()
    {
        pausePanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    // Nút back từ Settings về Pause
    public void OnClickBackToPause()
    {
        settingsPanel.SetActive(false);
        pausePanel.SetActive(true);
    }

    // Nút về House (load scene)
    public void OnClickBackToHouse()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameplaySceneName);
    }

}