using UnityEngine;
using TMPro;
using DG.Tweening;

public class FloatingText : MonoBehaviour
{
    public TextMeshProUGUI text;

    public void Setup(string content, Color color)
    {
        text.text = content;
        text.color = color;

        RectTransform rect = GetComponent<RectTransform>();

        Vector3 startPos = rect.anchoredPosition;

        rect.DOAnchorPosY(startPos.y + 50f, 0.8f);
        text.DOFade(0f, 0.8f);

        Destroy(gameObject, 0.8f);
    }
}