using UnityEngine;
using System;
using System.Collections;

public class JsonDataManager : Singleton<JsonDataManager>
{
    public int gold { get; private set; }
    public int wood { get; private set; }
    public float hp { get; private set; }

    // ===== EVENT =====
    public event Action<int> OnGoldChanged;
    public event Action<int> OnWoodChanged;
    public event Action<float> OnHPChanged;

    protected override void Awake()
    {
        base.Awake();
    }

    // ===== LOAD DATA (có progress) =====
    public IEnumerator LoadData(Action<float> onProgress)
    {
        float progress = 0f;

        // giả lập load từng bước
        while (progress < 1f)
        {
            progress += Time.deltaTime;
            onProgress?.Invoke(progress);
            yield return null;
        }

        // dữ liệu sau khi load (sau này thay bằng JSON thật)
        SetGold(100);
        SetWood(50);
        SetHP(1f);
    }

    // ===== SET DATA =====

    public void SetGold(int value)
    {
        if (gold == value) return;

        gold = value;
        OnGoldChanged?.Invoke(gold);
    }

    public void SetWood(int value)
    {
        if (wood == value) return;

        wood = value;
        OnWoodChanged?.Invoke(wood);
    }

    public void SetHP(float value)
    {
        value = Mathf.Clamp01(value);

        if (Mathf.Approximately(hp, value)) return;

        hp = value;
        OnHPChanged?.Invoke(hp);
    }
}