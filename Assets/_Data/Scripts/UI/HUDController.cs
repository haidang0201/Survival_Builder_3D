using UnityEngine;
using TMPro;
using DG.Tweening;

/*
 * HUDController.cs
 * Folder: Scripts/UI/
 * Người làm: VŨ
 *
 * Hiển thị tài nguyên trên HUD (Top bar).
 * Nhận lệnh cập nhật từ UIResourceObserver – KHÔNG tự subscribe event.
 *
 * API chuẩn (UIResourceObserver gọi):
 *   UpdateGold (int value)
 *   UpdateWood (int current, int max)
 *   UpdateStone(int current, int max)
 *   UpdateFood (int current, int max)
 *
 * Hiệu ứng DOTween:
 *   - Số đếm mượt (AnimateNumber)
 *   - Scale pulse khi tăng
 *   - Shake + đỏ khi giảm
 *   - Floating text +/- tại vị trí text
 */

public class HUDController : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // INSPECTOR
    // ──────────────────────────────────────────────

    [Header("Top UI – Tài nguyên")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI woodText;
    public TextMeshProUGUI stoneText;
    public TextMeshProUGUI foodText;     // Có thể null nếu chưa có UI food

    [Header("Floating Text FX")]
    public GameObject floatingTextPrefab;
    public Transform floatingTextParent;

    // ──────────────────────────────────────────────
    // PRIVATE STATE  (theo dõi giá trị cũ để tính delta)
    // ──────────────────────────────────────────────

    private int _currentGold;
    private int _currentWood;
    private int _currentStone;
    private int _currentFood;

    // ──────────────────────────────────────────────
    // LIFECYCLE
    // ──────────────────────────────────────────────

    private void Start()
    {
        // Hiển thị 0 ngay khi khởi động, UIResourceObserver sẽ push giá trị thật sau
        SetGoldText(0);
        SetResourceText(woodText, 0, 0);
        SetResourceText(stoneText, 0, 0);
        SetResourceText(foodText, 0, 0);
    }

    // ──────────────────────────────────────────────
    // PUBLIC API  –  UIResourceObserver gọi
    // ──────────────────────────────────────────────

    /// <summary>Cập nhật vàng (không có max cap).</summary>
    public void UpdateGold(int value)
    {
        int delta = value - _currentGold;
        _currentGold = value;

        AnimateNumber(goldText, _currentGold - delta, value);

        if (delta != 0)
        {
            ShowFloatingText(delta, goldText, new Color(1f, 0.85f, 0f)); // Màu vàng
            PulseOrShake(goldText, delta);
        }
    }

    /// <summary>Cập nhật gỗ với sức chứa → hiển thị "current / max".</summary>
    public void UpdateWood(int current, int max)
    {
        int delta = current - _currentWood;
        _currentWood = current;

        AnimateResource(woodText, _currentWood - delta, current, max);

        if (delta != 0)
        {
            ShowFloatingText(delta, woodText, new Color(0.6f, 0.35f, 0.1f)); // Nâu gỗ
            PulseOrShake(woodText, delta);
        }
    }

    /// <summary>Cập nhật đá với sức chứa → hiển thị "current / max".</summary>
    public void UpdateStone(int current, int max)
    {
        int delta = current - _currentStone;
        _currentStone = current;

        AnimateResource(stoneText, _currentStone - delta, current, max);

        if (delta != 0)
        {
            ShowFloatingText(delta, stoneText, Color.gray);
            PulseOrShake(stoneText, delta);
        }
    }

    /// <summary>Cập nhật lương thực với sức chứa → hiển thị "current / max".</summary>
    public void UpdateFood(int current, int max)
    {
        if (foodText == null) return;

        int delta = current - _currentFood;
        _currentFood = current;

        AnimateResource(foodText, _currentFood - delta, current, max);

        if (delta != 0)
        {
            ShowFloatingText(delta, foodText, new Color(0.2f, 0.8f, 0.2f)); // Xanh lá
            PulseOrShake(foodText, delta);
        }
    }

    // ──────────────────────────────────────────────
    // PRIVATE – ANIMATION
    // ──────────────────────────────────────────────

    /// <summary>Đếm số mượt từ from → to (dùng cho gold, không có max).</summary>
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

    /// <summary>Đếm số mượt từ from → to, giữ nguyên phần "/max".</summary>
    private void AnimateResource(TextMeshProUGUI text, int from, int to, int max)
    {
        if (text == null) return;
        int temp = from;
        DOTween.To(() => temp, x =>
        {
            temp = x;
            text.text = $"{x} / {max}";
        }, to, 0.3f).SetEase(Ease.OutCubic);
    }

    /// <summary>Set text tức thì không animation (dùng khi init).</summary>
    private void SetGoldText(int value)
    {
        if (goldText != null) goldText.text = value.ToString();
    }

    private void SetResourceText(TextMeshProUGUI text, int current, int max)
    {
        if (text != null) text.text = $"{current} / {max}";
    }

    /// <summary>Scale pulse khi tăng, shake + đỏ khi giảm.</summary>
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