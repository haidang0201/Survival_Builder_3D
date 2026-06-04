using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

/*
 * HUDController.cs
 * Folder: Scripts/UI/
 * Dự án: KHẨN HOANG (PENTA DEV)
 * Người thực hiện: VŨ (Phiên bản Ultimate Optimization - Tối ưu tuyệt đối)
 *
 * NHIỆM VỤ: Hiển thị tài nguyên trên HUD dạng số nguyên, tích hợp Object Pool 
 * và cơ chế Gộp dữ liệu (Throttling) để chống lag tuyệt đối khi thay đổi số lượng lớn.
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

    // --- HỆ THỐNG OBJECT POOL MINI ---
    private Queue<GameObject> _floatingTextPool = new Queue<GameObject>();

    // --- CẤU TRÚC GỘP TÀI NGUYÊN (CHỐNG SPAM REBUILD MESH UI) ---
    private Dictionary<TextMeshProUGUI, int> _pendingDeltas = new Dictionary<TextMeshProUGUI, int>();
    private Dictionary<TextMeshProUGUI, float> _cooldownTimers = new Dictionary<TextMeshProUGUI, float>();
    private const float UI_REFRESH_COOLDOWN = 0.05f; // Chờ 0.05s để gộp các tài nguyên thay đổi quá nhanh

    private void Start()
    {
        SetTextInstant(goldText, 0);
        SetTextInstant(woodText, 0);
        SetTextInstant(stoneText, 0);
        SetTextInstant(foodText, 0);
    }

    private void Update()
    {
        // Xử lý bộ đếm thời gian gộp tài nguyên
        List<TextMeshProUGUI> keys = new List<TextMeshProUGUI>(_cooldownTimers.Keys);
        foreach (var textKey in keys)
        {
            if (_cooldownTimers[textKey] > 0)
            {
                _cooldownTimers[textKey] -= Time.deltaTime;
                if (_cooldownTimers[textKey] <= 0 && _pendingDeltas[textKey] != 0)
                {
                    // Hết thời gian chờ -> Tiến hành kích hoạt hiệu ứng Text bay một lần duy nhất cho tổng số dư
                    TriggerFloatingTextAndFx(textKey, _pendingDeltas[textKey]);
                    _pendingDeltas[textKey] = 0; // Reset số dư chờ
                }
            }
        }
    }

    // --- CÁC HÀM CẬP NHẬT TỪ EVENT QUẢN LÝ ---

    public void UpdateGold(int value)
    {
        int delta = value - _currentGold;
        _currentGold = value;

        AnimateNumber(goldText, value);
        HandleResourceChange(goldText, delta);
    }

    public void UpdateWood(int value)
    {
        int delta = value - _currentWood;
        _currentWood = value;

        AnimateNumber(woodText, value);
        HandleResourceChange(woodText, delta);
    }

    public void UpdateStone(int value)
    {
        int delta = value - _currentStone;
        _currentStone = value;

        AnimateNumber(stoneText, value);
        HandleResourceChange(stoneText, delta);
    }

    public void UpdateFood(int value)
    {
        if (foodText == null) return;

        int delta = value - _currentFood;
        _currentFood = value;

        AnimateNumber(foodText, value);
        HandleResourceChange(foodText, delta);
    }

    // ──────────────────────────────────────────────────────────────
    // LOGIC XỬ LÝ GỘP DỮ LIỆU (THROTTLING LOGIC)
    // ──────────────────────────────────────────────────────────────

    private void HandleResourceChange(TextMeshProUGUI textTarget, int delta)
    {
        if (delta == 0 || textTarget == null) return;

        // Khởi tạo nếu chưa có trong Dictionary
        if (!_pendingDeltas.ContainsKey(textTarget)) _pendingDeltas[textTarget] = 0;
        if (!_cooldownTimers.ContainsKey(textTarget)) _cooldownTimers[textTarget] = 0f;

        // Cộng dồn delta (ví dụ nhấn phím liên tục thì số sẽ tích lũy lại thay vì sinh nhiều text bay)
        _pendingDeltas[textTarget] += delta;

        // Nếu không trong thời gian chờ cooldown, xử lý ngay lập tức
        if (_cooldownTimers[textTarget] <= 0f)
        {
            TriggerFloatingTextAndFx(textTarget, _pendingDeltas[textTarget]);
            _pendingDeltas[textTarget] = 0;
            _cooldownTimers[textTarget] = UI_REFRESH_COOLDOWN; // Đặt thời gian đóng băng tạm thời
        }
    }

    private void TriggerFloatingTextAndFx(TextMeshProUGUI textTarget, int totalDelta)
    {
        if (totalDelta == 0) return;

        // Lấy màu sắc đặc trưng theo từng loại Text tài nguyên
        Color fxColor = Color.white;
        if (textTarget == goldText) fxColor = new Color(1f, 0.85f, 0f);
        else if (textTarget == woodText) fxColor = new Color(0.6f, 0.35f, 0.1f);
        else if (textTarget == stoneText) fxColor = Color.gray;
        else if (textTarget == foodText) fxColor = new Color(0.2f, 0.8f, 0.2f);

        // Kích hoạt hiển thị Text Bay bằng Pool
        ShowFloatingTextOptimized(totalDelta, textTarget, fxColor);
        
        // Kích hoạt hiệu ứng Co giãn/Nháy màu HUD
        PulseOrShake(textTarget, totalDelta);
    }

    // ──────────────────────────────────────────────────────────────
    // PRIVATE – ANIMATION & POOL (TỐI ƯU ĐỒ HỌA)
    // ──────────────────────────────────────────────────────────────

    private void AnimateNumber(TextMeshProUGUI text, int to)
    {
        if (text == null) return;

        // Ép DOTween dừng luồng chạy số cũ trên Text này để không lỗi Rebuild Mesh UI
        DOTween.Kill(text);

        int temp = int.Parse(text.text);
        DOTween.To(() => temp, x =>
        {
            temp = x;
            text.text = x.ToString();
        }, to, 0.15f)
        .SetEase(Ease.OutCubic)
        .SetId(text);
    }

    private void SetTextInstant(TextMeshProUGUI text, int value)
    {
        if (text != null) text.text = value.ToString();
    }

    private void PulseOrShake(TextMeshProUGUI text, int delta)
    {
        if (text == null) return;

        DOTween.Kill(text.transform);
        text.transform.localScale = Vector3.one;

        if (delta > 0)
        {
            text.transform.DOScale(1.2f, 0.1f)
                .SetLoops(2, LoopType.Yoyo)
                .SetId(text.transform);
        }
        else
        {
            text.transform.DOShakeScale(0.15f, 0.3f).SetId(text.transform);
            
            DOTween.Kill(text, true);
            text.DOColor(Color.red, 0.08f)
                .OnComplete(() => text.DOColor(Color.white, 0.12f))
                .SetId(text);
        }
    }

    private void ShowFloatingTextOptimized(int amount, TextMeshProUGUI anchor, Color color)
    {
        if (floatingTextPrefab == null || floatingTextParent == null || anchor == null) return;

        GameObject obj = null;

        if (_floatingTextPool.Count > 0)
        {
            obj = _floatingTextPool.Dequeue();
            if (obj == null) obj = Instantiate(floatingTextPrefab, floatingTextParent);
        }
        else
        {
            obj = Instantiate(floatingTextPrefab, floatingTextParent);
        }

        obj.SetActive(true);
        obj.transform.position = anchor.transform.position;

        var ft = obj.GetComponent<FloatingText>();
        if (ft != null)
        {
            string prefix = amount > 0 ? "+" : "";
            ft.Setup(prefix + amount, color);
        }

        // Tạo hiệu ứng chuyển động mượt bằng DOTween và tự thu hồi về Pool
        obj.transform.DOComplete();
        obj.transform.DOMoveY(obj.transform.position.y + 35f, 0.5f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                obj.SetActive(false);
                _floatingTextPool.Enqueue(obj);
            });
    }

    private void OnDestroy()
    {
        // Giải phóng toàn bộ Tween tránh rò rỉ bộ nhớ (Memory Leak)
        DOTween.Kill(goldText);
        DOTween.Kill(woodText);
        DOTween.Kill(stoneText);
        DOTween.Kill(foodText);
        
        if (goldText != null) DOTween.Kill(goldText.transform);
        if (woodText != null) DOTween.Kill(woodText.transform);
        if (stoneText != null) DOTween.Kill(stoneText.transform);
        if (foodText != null) DOTween.Kill(foodText.transform);
    }
}