using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // Neu khong dung TextMeshPro, xoa dong nay va doi TMP_Text -> Text (UnityEngine.UI)

// Gan script nay vao GameObject "SplashLoadingManager".
// Keo Image BarFill (Image Type = Filled) vao "fillBar".
// Keo Text % (neu co) vao "percentText".
public class SplashLoadingBar : MonoBehaviour
{
    [Header("Thanh loading (Image Type = Filled)")]
    public Image fillBar;

    [Header("Text hien % (co the de trong)")]
    public TMP_Text percentText;

    [Header("Thoi gian chay gia (giay)")]
    public float duration = 2.5f;

    [Header("Duong cong tang toc do (de khong chay deu deu nhu robot)")]
    public AnimationCurve progressCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Tu dong chuyen scene sau khi chay xong")]
    public bool autoLoadNextScene = true;
    public string nextSceneName = "MainGame";

    void Start()
    {
        if (fillBar != null) fillBar.fillAmount = 0f;
        StartCoroutine(RunFakeLoading());
    }

    private IEnumerator RunFakeLoading()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float progress = progressCurve.Evaluate(t);

            if (fillBar != null)
                fillBar.fillAmount = progress;

            if (percentText != null)
                percentText.text = Mathf.RoundToInt(progress * 100f) + "%";

            yield return null;
        }

        // Dam bao ket thuc dung 100%
        if (fillBar != null) fillBar.fillAmount = 1f;
        if (percentText != null) percentText.text = "100%";

        yield return new WaitForSeconds(0.3f); // dung lai 1 chut cho nguoi choi thay 100%

        if (autoLoadNextScene && !string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}