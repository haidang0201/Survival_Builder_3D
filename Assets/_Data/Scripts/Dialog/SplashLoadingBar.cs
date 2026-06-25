using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SplashLoadingBar : MonoBehaviour
{
    [Header("Bar")]
    public Image fillBar;

    [Header("Text %")]
    public TMP_Text percentText;

    [Header("Time")]
    public float duration = 2.5f;

    [Header("Next Scene")]
    public bool autoLoadNextScene = true;
    public string nextSceneName = "MainGame";

    // 🔥 ADD: event sync intro
    public System.Action<float> OnLoadingProgress;

    void Start()
    {
        if (fillBar != null) fillBar.fillAmount = 0f;
        StartCoroutine(RunFakeLoading());
    }

    IEnumerator RunFakeLoading()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            fillBar.fillAmount = t;

            if (percentText != null)
                percentText.text = Mathf.RoundToInt(t * 100f) + "%";

            // 🔥 SYNC INTRO
            OnLoadingProgress?.Invoke(t);

            yield return null;
        }

        fillBar.fillAmount = 1f;
        if (percentText != null) percentText.text = "100%";

        yield return new WaitForSeconds(0.3f);

        if (autoLoadNextScene)
            SceneManager.LoadScene(nextSceneName);
    }
}