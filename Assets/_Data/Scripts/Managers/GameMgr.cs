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
        // Chuyển state worker, bật building interaction
    }

    void HandleNightStart()
    {
        Debug.Log("Bắt đầu đêm: tắt harvesting, bật defense");
        // Chuyển state worker sang combat, bật defense building
    }
}