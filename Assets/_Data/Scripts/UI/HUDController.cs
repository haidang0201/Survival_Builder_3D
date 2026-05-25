using UnityEngine;
using TMPro;
using DG.Tweening;

public class HUDController : MonoBehaviour
{
    [Header("Top UI Text (Resources)")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI woodText;
    public TextMeshProUGUI stoneText;

    [Header("Floating Text FX")]
    public GameObject floatingTextPrefab;
    public Transform floatingTextParent;

    private int currentWood;
    private int currentStone;

    private void Start()
    {
        // Khởi tạo hiển thị ban đầu với giá trị bằng 0
        UpdateGold(0);
        UpdateWood(0);
        UpdateStone(0);
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

    // ================= STONE =================
    public void UpdateStone(int value)
    {
        int oldValue = currentStone;
        currentStone = value;

        int delta = value - oldValue;

        AnimateNumber(stoneText, oldValue, value);

        if (delta != 0)
        {
            ShowFloatingText(delta, stoneText.transform.position, Color.gray);

            if (delta > 0)
            {
                stoneText.transform.DOScale(1.2f, 0.15f).SetLoops(2, LoopType.Yoyo);
            }
            else
            {
                stoneText.transform.DOShakeScale(0.3f, 0.5f);
                stoneText.DOColor(Color.red, 0.2f)
                    .OnComplete(() => stoneText.DOColor(Color.white, 0.2f));
            }
        }
    }

    // ================= SUPPORT FX =================

    void AnimateNumber(TextMeshProUGUI text, int from, int to)
    {
        if (text == null) return;
        
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
        if (ft != null)
        {
            string prefix = amount > 0 ? "+" : "";
            ft.Setup(prefix + amount.ToString(), color);
        }
    }
}