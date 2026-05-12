using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class HUDController : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI woodText;
    public Image healthFill;

    [Header("Floating Text")]
    public GameObject floatingTextPrefab;
    public Transform floatingTextParent;

    private int currentGold;
    private int currentWood;

    private void Start()
    {
        UpdateGold(0);
        UpdateWood(0);
        UpdateHealth(1f);
    }

    // ================= GOLD =================
    public void UpdateGold(int value)
    {
        int oldValue = currentGold;
        currentGold = value;

        int delta = value - oldValue;

        AnimateNumber(goldText, oldValue, value);

        if (delta != 0)
        {
            ShowFloatingText(delta, goldText.transform.position, Color.yellow);

            if (delta > 0)
            {
                goldText.transform.DOScale(1.2f, 0.15f).SetLoops(2, LoopType.Yoyo);
            }
            else
            {
                goldText.transform.DOShakeScale(0.3f, 0.5f);
                goldText.DOColor(Color.red, 0.2f)
                    .OnComplete(() => goldText.DOColor(Color.white, 0.2f));
            }
        }
    }

    // ================= WOOD =================
    public void UpdateWood(int value)
    {
        int oldValue = currentWood;
        currentWood = value;

        int delta = value - oldValue;

        AnimateNumber(woodText, oldValue, value);

        if (delta != 0)
        {
            ShowFloatingText(delta, woodText.transform.position, Color.green);

            if (delta > 0)
            {
                woodText.transform.DOScale(1.2f, 0.15f).SetLoops(2, LoopType.Yoyo);
            }
            else
            {
                woodText.transform.DOShakeScale(0.3f, 0.5f);
                woodText.DOColor(Color.red, 0.2f)
                    .OnComplete(() => woodText.DOColor(Color.white, 0.2f));
            }
        }
    }

    // ================= HEALTH =================
    public void UpdateHealth(float percent)
    {
        if (healthFill == null) return;

        healthFill.DOFillAmount(percent, 0.3f);

        healthFill.DOColor(Color.red, 0.1f)
            .OnComplete(() => healthFill.DOColor(Color.white, 0.2f));

        healthFill.rectTransform.DOShakeAnchorPos(0.2f, 10f);
    }

    // ================= SUPPORT =================

    void AnimateNumber(TextMeshProUGUI text, int from, int to)
    {
        DOTween.To(() => from, x =>
        {
            text.text = x.ToString();
        }, to, 0.3f);
    }

    void ShowFloatingText(int amount, Vector3 worldPos, Color color)
    {
        if (floatingTextPrefab == null || floatingTextParent == null) return;

        GameObject obj = Instantiate(floatingTextPrefab, floatingTextParent);

        obj.transform.position = worldPos;

        var ft = obj.GetComponent<FloatingText>();

        string prefix = amount > 0 ? "+" : "";
        ft.Setup(prefix + amount.ToString(), color);
    }

    // ===== HOOK SYSTEM =====
    // public void ConnectToSystem(float hpValue)
    // {
    //     LoadBehavior.Ins.UI.UpdateHealth(hpValue);
    // }
}