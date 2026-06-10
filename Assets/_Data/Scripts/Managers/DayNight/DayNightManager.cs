using System;
using UnityEngine;

public class DayNightManager : Singleton<DayNightManager>
{
    public enum Mode { Day, Night }
    public Mode CurrentMode { get; private set; } = Mode.Day;

    public event Action OnDayStart;
    public event Action OnNightStart;

    public float DayDuration = 15f;  // Thời gian ban ngày (giây)
    public float NightDuration = 30f; // Thời gian ban đêm (giây)

    private float timer;

    // ================= PHẦN CẬP NHẬT MỚI CHO UI =================
    public int CurrentDay { get; private set; } = 0; // Bộ đếm ngày (Bắt đầu từ Day 0 theo UI nhóm)
    public float CurrentTimer => timer;              // Đẩy thời gian đếm ngược ra cho UI đọc

    protected override void Awake()
    {
        base.Awake(); // Gọi Singleton.MakeSingleton
        timer = DayDuration;
        CurrentMode = Mode.Day;
        CurrentDay = 0; // Khởi đầu game ở Ngày 0
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
            // CHUYỂN SANG BAN ĐÊM: Đếm ngược theo thời lượng ban đêm
            CurrentMode = Mode.Night;
            timer = NightDuration;
            OnNightStart?.Invoke();
        }
        else
        {
            // CHUYỂN SANG BAN NGÀY: Đếm ngược theo thời lượng ban ngày
            CurrentMode = Mode.Day;
            timer = DayDuration;

            // ĐẶC BIỆT: Sang ngày mới -> Tự động tăng số ngày lên 1 (Day 0 -> Day 1 -> Day 2...)
            CurrentDay++;

            OnDayStart?.Invoke();
        }
    }

    public bool IsDay() => CurrentMode == Mode.Day;
    public bool IsNight() => CurrentMode == Mode.Night;
}