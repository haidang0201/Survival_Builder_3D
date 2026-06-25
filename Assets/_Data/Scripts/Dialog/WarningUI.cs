using UnityEngine;
using TMPro;

public class WarningUI : MonoBehaviour
{
    public static WarningUI Instance;

    public GameObject panel;
    public TextMeshProUGUI text;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public void Show(string msg)
    {
        panel.SetActive(true);
        text.text = msg;
    }

    public void Hide()
    {
        panel.SetActive(false);
    }
}