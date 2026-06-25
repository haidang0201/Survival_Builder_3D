using UnityEngine;

public class DayNightIconUI : MonoBehaviour
{
    [Header("STACKED ICON (SAME POSITION)")]
    public GameObject dayIcon;
    public GameObject nightIcon;

    [Header("REFERENCE")]
    public DayNightManager dayNight;

    void Start()
    {
        UpdateIcon();
    }

    void Update()
    {
        if (dayNight == null) return;

        UpdateIcon();
    }

    void UpdateIcon()
    {
        if (dayNight.IsDay())
        {
            if (dayIcon != null) dayIcon.SetActive(true);
            if (nightIcon != null) nightIcon.SetActive(false);
        }
        else
        {
            if (dayIcon != null) dayIcon.SetActive(false);
            if (nightIcon != null) nightIcon.SetActive(true);
        }
    }
}