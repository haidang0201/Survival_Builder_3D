using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class HUDController : MonoBehaviour
{
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI woodText;
    public Image healthFill;

    int currentGold;
    int currentWood;

    private void Start()
    {
        // trạng thái ban đầu
        UpdateGold(0);
        UpdateWood(0);
        UpdateHealth(1f);
    }

    public void UpdateGold(int value)
    {
        int start = currentGold;
        currentGold = value;

        DOTween.To(() => start, x =>
        {
            goldText.text = x.ToString();
        }, value, 0.5f);

        goldText.transform.DOScale(1.2f, 0.2f).SetLoops(2, LoopType.Yoyo);
    }

    public void UpdateWood(int value)
    {
        int start = currentWood;
        currentWood = value;

        DOTween.To(() => start, x =>
        {
            woodText.text = x.ToString();
        }, value, 0.5f);

        woodText.transform.DOScale(1.2f, 0.2f).SetLoops(2, LoopType.Yoyo);
    }

    public void UpdateHealth(float percent)
    {
         Debug.Log("HP: " + percent);
        healthFill.fillAmount = percent;
    }

    public void Shake()
    {
        transform.DOShakePosition(0.3f, 10f);
    }
}