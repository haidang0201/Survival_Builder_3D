using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class HUDController : MonoBehaviour
{
    [Header("UI")]
    //  public TextMeshProUGUI goldText;
    public TextMeshProUGUI woodText;
    public TextMeshProUGUI stoneText; // Kéo Text tương ứng vào đây trong Inspector
    public TextMeshProUGUI foodText;  // Kéo Text tương ứng vào đây trong Inspector
    public TextMeshProUGUI healthText;


    public Image healthFill;

    [Header("Floating Text")]
    public GameObject floatingTextPrefab;
    public Transform floatingTextParent;

    private int currentWood;
    public TMPro.TextMeshProUGUI[] resourceTexts;

    private void Start()
    {
        //UpdateGold(0);
        UpdateWood(0);
        UpdateHealth(1f);
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
    // Ví dụ mẫu trong HUDController.cs
    public void UpdateWood(int current, int max)
    {
        woodText.text = $"{current} / {max}";
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
    public void UpdateStone(int current, int max)
    {
        if (stoneText != null)
        {
            stoneText.text = $"{current} / {max}";
        }
    }

    // Hàm Update Lúa
    public void UpdateFood(int current, int max)
    {
        if (foodText != null)
        {
            foodText.text = $"{current} / {max}";
        }
    }
    public void SetTextColor(Color newColor)
    {
        foreach (var txt in resourceTexts)
        {
            if (txt != null) txt.color = newColor;
        }
    }

    // ===== HOOK SYSTEM =====
    // public void ConnectToSystem(float hpValue)
    // {
    //     LoadBehavior.Ins.UI.UpdateHealth(hpValue);
    // }
}