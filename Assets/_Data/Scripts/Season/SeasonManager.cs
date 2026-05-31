using UnityEngine;
using System;

public enum SeasonType
{
    Xuan,
    He,
    Thu,
    Lanh
}

public class SeasonManager : MonoBehaviour
{
    public static SeasonManager Instance { get; private set; }

    [Header("Mùa hiện tại")]
    public SeasonType currentSeason;

    // Sự kiện phát loa thông báo
    public static event Action<SeasonType> OnSeasonChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Hàm này nhận lệnh từ bộ đếm thời gian và phát loa cho toàn Game
    public void SetSeason(SeasonType newSeason)
    {
        currentSeason = newSeason;
        OnSeasonChanged?.Invoke(currentSeason);
    }
}