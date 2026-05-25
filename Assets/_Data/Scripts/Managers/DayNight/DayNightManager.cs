using System;
using UnityEngine;

public class DayNightManager : Singleton<DayNightManager>
{
    public enum Mode { Day, Night }
    public Mode CurrentMode { get; private set; } = Mode.Day;

    public event Action OnDayStart;
    public event Action OnNightStart;

    public float DayDuration = 15f;  // seconds
    public float NightDuration = 12f;

    private float timer;

    protected override void Awake()
    {
        base.Awake(); // Gọi Singleton.MakeSingleton
        timer = DayDuration;
        CurrentMode = Mode.Day;
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            SwitchMode();
        }
    }

    private void SwitchMode()
    {
        if (CurrentMode == Mode.Day)
        {
            CurrentMode = Mode.Night;
            timer = NightDuration;
            OnNightStart?.Invoke();  // Kích hoạt event cho hệ thống khác
        }
        else
        {
            CurrentMode = Mode.Day;
            timer = DayDuration;
            OnDayStart?.Invoke();    // Kích hoạt event cho hệ thống khác
        }
    }

    // Optional helper để check mode
    public bool IsDay() => CurrentMode == Mode.Day;
    public bool IsNight() => CurrentMode == Mode.Night;
}