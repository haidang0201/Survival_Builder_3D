using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingUI : MonoBehaviour
{
    public GameObject panel;
    public Slider progressBar;
    public TextMeshProUGUI loadingText;

    public void Show()
    {
        panel.SetActive(true);
        SetProgress(0f);
    }

    public void SetProgress(float value)
    {
        value = Mathf.Clamp01(value);

        progressBar.value = value;
        loadingText.text = "Loading... " + Mathf.RoundToInt(value * 100) + "%";
    }

    public void Hide()
    {
        panel.SetActive(false);
    }
}