using UnityEngine;
using UnityEngine.UI;

public class QuestTabObjectSwitcher : MonoBehaviour
{
    [Header("BUTTONS")]
    public Button thietLapBtn;
    public Button toiUuBtn;
    public Button anNinhBtn;

    [Header("OBJECTS")]
    public GameObject objThietLap;   // chỉ hiện lúc đầu
    public GameObject objToiUu_1;    // hiện khi tối ưu
    public GameObject objToiUu_2;    // hiện khi tối ưu

    void Start()
    {
        thietLapBtn.onClick.AddListener(ShowThietLap);
        toiUuBtn.onClick.AddListener(ShowToiUu);
        anNinhBtn.onClick.AddListener(HideAll);

        // 🔥 trạng thái ban đầu
        ShowThietLap();
    }

    // ================= THIẾT LẬP =================
    void ShowThietLap()
    {
        if (objThietLap != null)
            objThietLap.SetActive(true);

        if (objToiUu_1 != null)
            objToiUu_1.SetActive(false);

        if (objToiUu_2 != null)
            objToiUu_2.SetActive(false);
    }

    // ================= TỐI ƯU =================
    void ShowToiUu()
    {
        if (objThietLap != null)
            objThietLap.SetActive(false);

        if (objToiUu_1 != null)
            objToiUu_1.SetActive(true);

        if (objToiUu_2 != null)
            objToiUu_2.SetActive(true);
    }

    // ================= AN NINH =================
    void HideAll()
    {
        if (objThietLap != null)
            objThietLap.SetActive(false);

        if (objToiUu_1 != null)
            objToiUu_1.SetActive(false);

        if (objToiUu_2 != null)
            objToiUu_2.SetActive(false);
    }
}