using System.Collections;
using UnityEngine;
using TMPro;

public class GameIntroCinematicFX : MonoBehaviour
{
    [Header("UI")]
    public GameObject introPanel;
    public TextMeshProUGUI introText;

    [Header("FX SETTINGS")]
    public float typeSpeed = 0.02f;
    public float holdTime = 1.2f;

    [Header("VISUAL FX")]
    public float pulseSpeed = 3f;
    public float pulseScale = 1.08f;
    public float fadeSpeed = 4f;

    Vector3 baseScale;
    Color baseColor;

    void Start()
    {
        baseScale = introText.transform.localScale;
        baseColor = introText.color;

        StartCoroutine(PlayIntro());
        StartCoroutine(PulseEffect());
    }

    IEnumerator PlayIntro()
    {
        introPanel.SetActive(true);

        yield return ShowLine("Vùng đất này từng là nơi phồn vinh...");
        yield return ShowLine("Nhưng chiến tranh đã xé nát tất cả...");
        yield return ShowLine("Chỉ còn lại những ngôi làng hoang tàn...");
        yield return ShowLine("Ngươi là người dẫn dắt cuối cùng...");
        yield return ShowLine("Xây dựng lại vùng đất này...");
        yield return ShowLine("Chống lại kẻ xâm lược...");
        yield return ShowLine("Và viết lại lịch sử của chính mình...");

        yield return new WaitForSeconds(1.5f);

        introPanel.SetActive(false);
    }

    IEnumerator ShowLine(string msg)
    {
        introText.text = "";
        introText.transform.localScale = baseScale;

        // 🔥 FADE IN START
        yield return StartCoroutine(Fade(0, 1));

        foreach (char c in msg)
        {
            introText.text += c;

            // nhẹ shake khi chữ xuất hiện
            introText.transform.localPosition =
                new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0);

            yield return new WaitForSeconds(typeSpeed);
        }

        introText.transform.localPosition = Vector3.zero;

        yield return new WaitForSeconds(holdTime);

        yield return StartCoroutine(Fade(1, 0));
    }

    IEnumerator Fade(float from, float to)
    {
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * fadeSpeed;

            Color c = introText.color;
            c.a = Mathf.Lerp(from, to, t);
            introText.color = c;

            yield return null;
        }
    }

    IEnumerator PulseEffect()
    {
        while (true)
        {
            float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * 0.03f;
            introText.transform.localScale = baseScale * scale;

            yield return null;
        }
    }
}