using UnityEngine;
using TMPro;
using DG.Tweening;

/*
 * HUDController.cs
 * Folder: Scripts/UI/
 * Người làm: VŨ
 *
 * Hiển thị tài nguyên trên HUD (Top bar) dạng số nguyên.
 */

public class HUDController : MonoBehaviour
{
    [Header("Top UI – Tài nguyên")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI woodText;
    public TextMeshProUGUI stoneText;
    public TextMeshProUGUI foodText;

    [Header("Floating Text FX")]
    public GameObject floatingTextPrefab;
    public Transform floatingTextParent;

    private int _currentGold;
    private int _currentWood;
    private int _currentStone;
    private int _currentFood;

    private void Start()
    {
        SetTextInstant(goldText, 0);
        SetTextInstant(woodText, 0);
        SetTextInstant(stoneText, 0);
        SetTextInstant(foodText, 0);
    }

    public void UpdateGold(int value)
    {
        int delta = value - _currentGold;
        _currentGold = value;

        AnimateNumber(goldText, _currentGold - delta, value);

        if (delta != 0)
        {
            ShowFloatingText(delta, goldText, new Color(1f, 0.85f, 0f)); // Vàng
            PulseOrShake(goldText, delta);
        }
    }

    public void UpdateWood(int value)
    {
        int delta = value - _currentWood;
        _currentWood = value;

        AnimateNumber(woodText, _currentWood - delta, value);

        if (delta != 0)
        {
            ShowFloatingText(delta, woodText, new Color(0.6f, 0.35f, 0.1f)); // Nâu gỗ
            PulseOrShake(woodText, delta);
        }
    }

    public void UpdateStone(int value)
    {
        int delta = value - _currentStone;
        _currentStone = value;

        AnimateNumber(stoneText, _currentStone - delta, value);

        if (delta != 0)
        {
            ShowFloatingText(delta, stoneText, Color.gray);
            PulseOrShake(stoneText, delta);
        }
    }

    public void UpdateFood(int value)
    {
        if (foodText == null) return;

        int delta = value - _currentFood;
        _currentFood = value;

        AnimateNumber(foodText, _currentFood - delta, value);

        if (delta != 0)
        {
            ShowFloatingText(delta, foodText, new Color(0.2f, 0.8f, 0.2f)); // Xanh lá
            PulseOrShake(foodText, delta);
        }
    }

    // ──────────────────────────────────────────────
    // PRIVATE – ANIMATION
    // ──────────────────────────────────────────────

    private void AnimateNumber(TextMeshProUGUI text, int from, int to)
    {
        if (text == null) return;
        int temp = from;
        DOTween.To(() => temp, x =>
        {
            temp = x;
            text.text = x.ToString();
        }, to, 0.3f).SetEase(Ease.OutCubic);
    }

    private void SetTextInstant(TextMeshProUGUI text, int value)
    {
        if (text != null) text.text = value.ToString();
    }

    private void PulseOrShake(TextMeshProUGUI text, int delta)
    {
        if (text == null) return;

        if (delta > 0)
        {
            text.transform.DOScale(1.2f, 0.15f).SetLoops(2, LoopType.Yoyo);
        }
        else
        {
            text.transform.DOShakeScale(0.3f, 0.5f);
            text.DOColor(Color.red, 0.2f)
                .OnComplete(() => text.DOColor(Color.white, 0.2f));
        }
    }

    private void ShowFloatingText(int amount, TextMeshProUGUI anchor, Color color)
    {
        if (floatingTextPrefab == null || floatingTextParent == null || anchor == null) return;

        GameObject obj = Instantiate(floatingTextPrefab, floatingTextParent);
        obj.transform.position = anchor.transform.position;

        var ft = obj.GetComponent<FloatingText>();
        if (ft != null)
        {
            string prefix = amount > 0 ? "+" : "";
            ft.Setup(prefix + amount, color);
        }
    }
}