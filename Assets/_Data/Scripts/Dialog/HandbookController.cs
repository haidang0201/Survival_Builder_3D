using UnityEngine;
using UnityEngine.UI;

public class HandbookController : MonoBehaviour
{
    public static HandbookController Instance;

    [Header("References")]
    public GameObject handbookPanel;
    public Button closeButton;
    public CanvasGroup canvasGroup;

    [Header("Settings")]
    public float fadeDuration = 0.3f;

    private bool isOpen = false;

    void Awake()
    {
        Instance = this;
        handbookPanel.SetActive(false);
    }

    void Start()
    {
        closeButton.onClick.AddListener(Hide);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("H pressed - isOpen: " + isOpen);
            Toggle();
        }
    }

    public void Show()
    {
        handbookPanel.SetActive(true);
        isOpen = true;
        StopAllCoroutines();
        StartCoroutine(FadeIn());
    }

    public void Hide()
    {
        StopAllCoroutines();
        StartCoroutine(FadeOutAndClose());
    }

    public void Toggle()
    {
        if (isOpen) Hide();
        else Show();
    }

    System.Collections.IEnumerator FadeIn()
    {
        float t = 0;
        canvasGroup.alpha = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = t / fadeDuration;
            yield return null;
        }
        canvasGroup.alpha = 1;
    }

    System.Collections.IEnumerator FadeOutAndClose()
    {
        float t = fadeDuration;
        while (t > 0)
        {
            t -= Time.deltaTime;
            canvasGroup.alpha = t / fadeDuration;
            yield return null;
        }
        canvasGroup.alpha = 0;
        handbookPanel.SetActive(false);
        isOpen = false;
    }
}