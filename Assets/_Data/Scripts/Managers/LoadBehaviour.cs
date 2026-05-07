using UnityEngine;

public class LoadBehavior : Singleton<LoadBehavior>
{
    [Header("Managers")]
    public GameMgr Game;
    public SoundMgr Sound;

    [Header("Systems")]
    public JsonDataManager Data;

    private bool initialized;

    protected override void Awake()
    {
        MakeSingleton(false);

        if (LoadBehavior.Ins != this)
        {
            enabled = false;
            return;
        }

        LoadManagers();
    }

    private void Start()
    {
        InitSystems();
    }

    private void LoadManagers()
    {
        if (Game == null)
            Game = FindObjectOfType<GameMgr>();

        if (Sound == null)
            Sound = FindObjectOfType<SoundMgr>();

        if (Data == null)
            Data = FindObjectOfType<JsonDataManager>();
    }

    private void InitSystems()
    {
        if (initialized) return;
        initialized = true;

        if (Data != null)
        {
            Data.LoadGame();
        }

        Debug.Log("LoadBehavior initialized.");
    }
}