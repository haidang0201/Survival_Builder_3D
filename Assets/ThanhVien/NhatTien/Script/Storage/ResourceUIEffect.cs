using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ResourceUIEffect : MonoBehaviour
{
    [Header("Reference — Kho chính")]
    public WarehouseStorage warehouseStorage;

    [Header("UI — Gỗ")]
    public TMP_Text woodText;
    public Image    woodIcon;

    [Header("UI — Lúa")]
    public TMP_Text riceText;
    public Image    riceIcon;

    [Header("UI — Đá")]
    public TMP_Text stoneText;
    public Image    stoneIcon;

    [Header("Tween Settings")]
    public float countDuration = 0.4f;
    public float punchStrength = 0.25f;
    public float punchDuration = 0.3f;
    public int   punchVibrato  = 5;

    // Dùng field riêng thay vì ref — tránh lỗi lambda CS1628
    private float displayWood  = 0f;
    private float displayRice  = 0f;
    private float displayStone = 0f;

    private Tween tweenWood;
    private Tween tweenRice;
    private Tween tweenStone;

    // ===== LIFECYCLE =====

    void Start()
    {
        if (warehouseStorage != null)
        {
            warehouseStorage.onWoodChanged.AddListener(OnWoodChanged);
            warehouseStorage.onRiceChanged.AddListener(OnRiceChanged);
            warehouseStorage.onStoneChanged.AddListener(OnStoneChanged);
        }

        RefreshAll();
    }

    void OnDestroy()
    {
        if (warehouseStorage != null)
        {
            warehouseStorage.onWoodChanged.RemoveListener(OnWoodChanged);
            warehouseStorage.onRiceChanged.RemoveListener(OnRiceChanged);
            warehouseStorage.onStoneChanged.RemoveListener(OnStoneChanged);
        }

        tweenWood?.Kill();
        tweenRice?.Kill();
        tweenStone?.Kill();
    }

    // ===== EVENT HANDLERS =====

    public void OnWoodChanged(int newAmount)
    {
        bool increased = newAmount > Mathf.RoundToInt(displayWood);

        tweenWood?.Kill();
        float start = displayWood;
        tweenWood = DOTween.To(
            ()  => start,
            x   => { start = x; displayWood = x; if (woodText != null) woodText.text = Mathf.RoundToInt(x).ToString(); },
            (float)newAmount,
            countDuration
        ).SetEase(Ease.OutQuad);

        if (increased && woodIcon != null)
        {
            woodIcon.transform.DOKill();
            woodIcon.transform.DOPunchScale(Vector3.one * punchStrength, punchDuration, punchVibrato, 1f);
        }
    }

    public void OnRiceChanged(int newAmount)
    {
        bool increased = newAmount > Mathf.RoundToInt(displayRice);

        tweenRice?.Kill();
        float start = displayRice;
        tweenRice = DOTween.To(
            ()  => start,
            x   => { start = x; displayRice = x; if (riceText != null) riceText.text = Mathf.RoundToInt(x).ToString(); },
            (float)newAmount,
            countDuration
        ).SetEase(Ease.OutQuad);

        if (increased && riceIcon != null)
        {
            riceIcon.transform.DOKill();
            riceIcon.transform.DOPunchScale(Vector3.one * punchStrength, punchDuration, punchVibrato, 1f);
        }
    }

    public void OnStoneChanged(int newAmount)
    {
        bool increased = newAmount > Mathf.RoundToInt(displayStone);

        tweenStone?.Kill();
        float start = displayStone;
        tweenStone = DOTween.To(
            ()  => start,
            x   => { start = x; displayStone = x; if (stoneText != null) stoneText.text = Mathf.RoundToInt(x).ToString(); },
            (float)newAmount,
            countDuration
        ).SetEase(Ease.OutQuad);

        if (increased && stoneIcon != null)
        {
            stoneIcon.transform.DOKill();
            stoneIcon.transform.DOPunchScale(Vector3.one * punchStrength, punchDuration, punchVibrato, 1f);
        }
    }

    // ===== UTILS =====

    public void RefreshAll()
    {
        if (warehouseStorage == null) return;

        displayWood  = warehouseStorage.CurrentWood;
        displayRice  = warehouseStorage.CurrentRice;
        displayStone = warehouseStorage.CurrentStone;

        if (woodText  != null) woodText.text  = warehouseStorage.CurrentWood.ToString();
        if (riceText  != null) riceText.text  = warehouseStorage.CurrentRice.ToString();
        if (stoneText != null) stoneText.text = warehouseStorage.CurrentStone.ToString();
    }
}