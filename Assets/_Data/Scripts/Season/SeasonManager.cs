using UnityEngine;
using System;

public enum SeasonType
{
    Xuan,
    He,
    Thu,   // Mới thêm
    Dong,  // Đổi tên từ Lạnh
    Mua
}

public class SeasonManager : MonoBehaviour
{
    public static SeasonManager Instance { get; private set; }

    [Header("Mùa hiện tại")]
    public SeasonType currentSeason;

    public static event Action<SeasonType> OnSeasonChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SetSeason(SeasonType newSeason)
    {
        currentSeason = newSeason;
        OnSeasonChanged?.Invoke(currentSeason);
    }
}