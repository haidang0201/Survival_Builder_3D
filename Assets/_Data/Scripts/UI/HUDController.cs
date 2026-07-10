using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

/*
 * HUDController.cs
 * Folder: Scripts/UI/
 * Dự án: KHẨN HOANG (PENTA DEV)
 * Người thực hiện: VŨ (Phiên bản Ultimate Optimization - ĐÃ FIX LỖI EVENT HUD)
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
    private const float UI_REFRESH_COOLDOWN = 0.05f;
    public static HUDController Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Khởi tạo giá trị ban đầu trực tiếp từ dữ liệu thực tế
        if (JsonDataManager.Ins != null)
        {
            _currentGold = JsonDataManager.Ins.gold;
            _currentWood = JsonDataManager.Ins.wood;
            _currentStone = JsonDataManager.Ins.stone;
            _currentFood = JsonDataManager.Ins.food;
        }

        SetTextInstant(goldText, _currentGold);
        SetTextInstant(woodText, _currentWood);
        SetTextInstant(stoneText, _currentStone);
        SetTextInstant(foodText, _currentFood);

        Debug.Log("[HUDController] ✅ Khởi tạo HUD thành công với thông số ban đầu.");
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
                    TriggerFloatingTextAndFx(textKey, _pendingDeltas[textKey]);
                    _pendingDeltas[textKey] = 0;
                }
            }
        }
    }

    // --- CÁC HÀM CẬP NHẬT TỪ EVENT QUẢN LÝ ---

    public void UpdateGold(int value)
    {
        if (goldText == null) return;
        int delta = value - _currentGold;

        // SỬA TẠI ĐÂY: Truyền giá trị CŨ và giá trị MỚI vào thẳng hàm Animate
        AnimateNumber(goldText, _currentGold, value);
        _currentGold = value;

        HandleResourceChange(goldText, delta);
    }

    public void UpdateWood(int value)
    {
        if (woodText == null) return;
        int delta = value - _currentWood;

        AnimateNumber(woodText, _currentWood, value);
        _currentWood = value;

        HandleResourceChange(woodText, delta);
    }

    public void UpdateStone(int value)
    {
        if (stoneText == null) return;
        int delta = value - _currentStone;

        AnimateNumber(stoneText, _currentStone, value);
        _currentStone = value;

        HandleResourceChange(stoneText, delta);
    }

    public void UpdateFood(int value)
    {
        if (foodText == null) return;
        int delta = value - _currentFood;

        AnimateNumber(foodText, _currentFood, value);
        _currentFood = value;

        HandleResourceChange(foodText, delta);
    }
    // ──────────────────────────────────────────────────────────────
    // LOGIC XỬ LÝ GỘP DỮ LIỆU (THROTTLING LOGIC)
    // ──────────────────────────────────────────────────────────────

    private void HandleResourceChange(TextMeshProUGUI textTarget, int delta)
    {
        if (delta == 0 || textTarget == null) return;

        if (!_pendingDeltas.ContainsKey(textTarget)) _pendingDeltas[textTarget] = 0;
        if (!_cooldownTimers.ContainsKey(textTarget)) _cooldownTimers[textTarget] = 0f;

        _pendingDeltas[textTarget] += delta;

        if (_cooldownTimers[textTarget] <= 0f)
        {
            TriggerFloatingTextAndFx(textTarget, _pendingDeltas[textTarget]);
            _pendingDeltas[textTarget] = 0;
            _cooldownTimers[textTarget] = UI_REFRESH_COOLDOWN;
        }
    }

    private void TriggerFloatingTextAndFx(TextMeshProUGUI textTarget, int totalDelta)
    {
        if (totalDelta == 0) return;

        Color fxColor = Color.white;
        if (textTarget == goldText) fxColor = new Color(1f, 0.85f, 0f);
        else if (textTarget == woodText) fxColor = new Color(0.6f, 0.35f, 0.1f);
        else if (textTarget == stoneText) fxColor = Color.gray;
        else if (textTarget == foodText) fxColor = new Color(0.2f, 0.8f, 0.2f);

        ShowFloatingTextOptimized(totalDelta, textTarget, fxColor);
        PulseOrShake(textTarget, totalDelta);
    }

    // ──────────────────────────────────────────────────────────────
    // PRIVATE – ANIMATION & POOL (TỐI ƯU ĐỒ HỌA)
    // ──────────────────────────────────────────────────────────────

    private void AnimateNumber(TextMeshProUGUI text, int fromValue, int toValue)
    {
        if (text == null) return;

        // Ép hủy các Tween cũ đang chạy trên Text này để không bị đè dữ liệu
        DOTween.Kill(text);

        int temp = fromValue; // Dùng trực tiếp giá trị biến hệ thống làm gốc chạy số

        DOTween.To(() => temp, x =>
        {
            temp = x;
            text.text = x.ToString(); // Cập nhật trực tiếp số lên màn hình
        }, toValue, 0.2f) // Thời gian chạy số 0.2 giây
        .SetEase(Ease.OutQuad)
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
            text.transform.DOShakeScale(0.15f, 0.3f)
                .SetId(text.transform);
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