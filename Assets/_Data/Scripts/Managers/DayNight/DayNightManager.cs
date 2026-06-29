using System;
using UnityEngine;

public class DayNightManager : Singleton<DayNightManager>
{
    public enum Mode { Day, Night }
    public Mode CurrentMode { get; private set; } = Mode.Day;

    public event Action OnDayStart;
    public event Action OnNightStart;

    [Header("Cài đặt Thời gian (Giây)")]
    public float DayDuration = 10f;  // Thời gian ban ngày 
    public float NightDuration = 10f; // Thời gian ban đêm 

    [Header("Đồng hồ đếm ngược (Chỉ xem, đừng sửa)")]
    [SerializeField] private float timer; // <--- THÊM [SerializeField] VÀO ĐÂY ĐỂ HIỆN LÊN INSPECTOR

    [Header("Thông tin UI (Không chỉnh sửa)")]
    public int CurrentDay = 0;
    public float CurrentTimer => timer;

    protected override void Awake()
    {
        base.Awake(); // Gọi Singleton

        timer = DayDuration;
        CurrentMode = Mode.Day;
        CurrentDay = 0;

        Debug.Log($"[DayNightManager] Đã khởi tạo thành công! Bắt đầu Ngày {CurrentDay} - Thời lượng: {DayDuration}s");
    }

    private void Update()
    {
        // Đảm bảo chỉ có Manager chính mới chạy
        if (Ins != this) return;

        // Đếm ngược thời gian
        if (timer > 0)
        {
            timer -= Time.deltaTime;
        }
        else
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

            Debug.Log($"[DayNightManager] ---> ĐÃ CHUYỂN SANG ĐÊM! Bắt đầu đếm ngược: {NightDuration}s");
            OnNightStart?.Invoke();
        }
        else
        {
            CurrentMode = Mode.Day;
            timer = DayDuration;
            CurrentDay++;

            Debug.Log($"[DayNightManager] ---> ĐÃ CHUYỂN SANG NGÀY {CurrentDay}! Bắt đầu đếm ngược: {DayDuration}s");
            OnDayStart?.Invoke();
        }
    }

    public bool IsDay() => CurrentMode == Mode.Day;
    public bool IsNight() => CurrentMode == Mode.Night;
}