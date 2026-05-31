using UnityEngine;

public class SeasonVisuals : MonoBehaviour
{
    [Header("Kéo 4 Material màu cây tương ứng vào đây")]
    public Material matXuan;  // Xanh lá tươi
    public Material matHe;    // Xanh đậm
    public Material matThu;   // Xanh xỉn/ướt
    public Material matLanh;  // Phủ tuyết trắng

    private Renderer meshRenderer;

    private void Awake()
    {
        meshRenderer = GetComponent<Renderer>();
    }

    private void OnEnable()
    {
        SeasonManager.OnSeasonChanged += DoiMauCay;

        // Đồng bộ màu ngay khi cái cây vừa được đẻ ra (khi load map)
        if (SeasonManager.Instance != null)
        {
            DoiMauCay(SeasonManager.Instance.currentSeason);
        }
    }

    private void OnDisable()
    {
        SeasonManager.OnSeasonChanged -= DoiMauCay;
    }

    private void DoiMauCay(SeasonType muaMoi)
    {
        if (meshRenderer == null) return;

        switch (muaMoi)
        {
            case SeasonType.Xuan:
                if (matXuan != null) meshRenderer.material = matXuan;
                break;
            case SeasonType.He:
                if (matHe != null) meshRenderer.material = matHe;
                break;
            case SeasonType.Thu:
                if (matThu != null) meshRenderer.material = matThu;
                break;
            case SeasonType.Lanh:
                if (matLanh != null) meshRenderer.material = matLanh;
                break;
        }
    }
}