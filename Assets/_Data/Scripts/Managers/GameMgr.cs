using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Boot,
    Loading,
    MainMenu,
    Playing,
    Paused,
    GameOver
}

public class GameMgr : Singleton<GameMgr>
{
    [Header("Scene Names")]


    [Header("Main Game References")]
    public GameObject questUI;
    public QuestManager questManager;
    //public GameObject buildingSystem;   // hệ thống xây dựng (ban ngày)
    //public GameObject defenseSystem;    // hệ thống phòng thủ (ban đêm)

    public GameState CurrentState { get; private set; } = GameState.Boot;

    public event Action<GameState> OnGameStateChanged;

    protected override void Awake()
    {
        MakeSingleton(false);

        if (GameMgr.Ins != this)
        {
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        SetState(GameState.Loading);
        var manager = DayNightManager.Ins;
        manager.OnDayStart += HandleDayStart;
        manager.OnNightStart += HandleNightStart;

        if (questUI != null) questUI.SetActive(false);
    }

    public void SetState(GameState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;
        OnGameStateChanged?.Invoke(CurrentState);

        Debug.Log("Game State: " + CurrentState);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SetState(GameState.MainMenu);
        SceneManager.LoadScene(0);
    }

    public void LoadMainGame()
    {
        Time.timeScale = 1f;
        SetState(GameState.Playing);
        SceneManager.LoadScene(1);
    }

    /// <summary>
    /// Gọi từ StoryUIController.onStoryFinished sau khi xem hết cutscene mở đầu.
    /// </summary>
    public void StartMainGame()
    {
        Time.timeScale = 1f;
        SetState(GameState.Playing);

        if (questUI != null) questUI.SetActive(true);

        if (questManager != null)
            questManager.SetQuest("Tiêu diệt bọn cướp, mở rộng lãnh thổ, mang lại bình yên");

        // Vào game sẽ bắt đầu bằng ban ngày (xây dựng), tắt phòng thủ
        //if (buildingSystem != null) buildingSystem.SetActive(true);
        // if (defenseSystem != null) defenseSystem.SetActive(false);

        Debug.Log("Main game started after story intro.");
    }


    public void PauseGame()
    {
        if (CurrentState != GameState.Playing) return;

        Time.timeScale = 0f;
        SetState(GameState.Paused);
    }

    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused) return;

        Time.timeScale = 1f;
        SetState(GameState.Playing);
    }

    public void GameOver()
    {
        Time.timeScale = 0f;
        SetState(GameState.GameOver);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    void HandleDayStart()
    {
        Debug.Log("Bắt đầu ngày: bật harvesting, tắt defense");

        //if (buildingSystem != null) buildingSystem.SetActive(true);
        //if (defenseSystem != null) defenseSystem.SetActive(false);
    }

    void HandleNightStart()
    {
        Debug.Log("Bắt đầu đêm: tắt harvesting, bật defense");

        //if (buildingSystem != null) buildingSystem.SetActive(false);
        // if (defenseSystem != null) defenseSystem.SetActive(true);
    }
}