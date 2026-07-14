using UnityEngine;

public class DayNightIconUI : MonoBehaviour
{
    [Header("STACKED ICON (SAME POSITION)")]
    public GameObject dayIcon;
    public GameObject nightIcon;


    [Header("REFERENCE")]
    public DayNightManager dayNight;


    private bool lastDayState;


    void Awake()
    {
        FindDayNightManager();
    }


    void Start()
    {
        UpdateIcon(true);
    }


    void OnEnable()
    {
        FindDayNightManager();
    }


    void Update()
    {
        // fallback nếu DayNightManager spawn sau
        if (dayNight == null)
        {
            FindDayNightManager();
            return;
        }


        bool currentDay = dayNight.IsDay();


        // chỉ update khi trạng thái đổi
        if (currentDay != lastDayState)
        {
            UpdateIcon(false);
        }
    }



    void FindDayNightManager()
    {
        if (dayNight != null)
            return;


        dayNight = FindObjectOfType<DayNightManager>();


        if (dayNight == null)
        {
            Debug.LogWarning(
                "[DayNightIconUI] Không tìm thấy DayNightManager"
            );
        }
    }



    void UpdateIcon(bool force)
    {
        if (dayNight == null)
            return;


        bool isDay = dayNight.IsDay();


        if (!force && isDay == lastDayState)
            return;


        lastDayState = isDay;



        if (isDay)
        {
            if (dayIcon != null)
                dayIcon.SetActive(true);


            if (nightIcon != null)
                nightIcon.SetActive(false);
        }
        else
        {
            if (dayIcon != null)
                dayIcon.SetActive(false);


            if (nightIcon != null)
                nightIcon.SetActive(true);
        }


        Debug.Log(
            "[DayNightIconUI] Current: " +
            (isDay ? "DAY" : "NIGHT")
        );
    }
}