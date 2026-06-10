using UnityEngine;

public class SeasonVisuals : MonoBehaviour
{
    [Header("Đổi Màu Sắc Theo Mùa (Materials)")]
    public Material matXuan;
    public Material matHe;
    public Material matThu;   // Mới thêm cho mùa Thu
    public Material matDong;  // Đã đổi tên thành Đông

    private Renderer meshRenderer;

    private void Awake()
    {
        // Chỉ lấy Renderer để quản lý việc đổi Material (lớp sơn màu)
        meshRenderer = GetComponent<Renderer>();
    }

    private void OnEnable()
    {
        SeasonManager.OnSeasonChanged += DoiHinhAnhCay;

        // Chạy ngay 1 lần lúc mới bật game để cây cập nhật đúng mùa hiện tại
        if (SeasonManager.Instance != null)
        {
            DoiHinhAnhCay(SeasonManager.Instance.currentSeason);
        }
    }

    private void OnDisable()
    {
        SeasonManager.OnSeasonChanged -= DoiHinhAnhCay;
    }

    private void DoiHinhAnhCay(SeasonType muaMoi)
    {
        // Thay lớp sơn (Màu Material) khi chuyển mùa
        if (meshRenderer != null)
        {
            switch (muaMoi)
            {
                case SeasonType.Xuan: if (matXuan != null) meshRenderer.material = matXuan; break;
                case SeasonType.He: if (matHe != null) meshRenderer.material = matHe; break;
                case SeasonType.Thu: if (matThu != null) meshRenderer.material = matThu; break;
                case SeasonType.Dong: if (matDong != null) meshRenderer.material = matDong; break;
            }
        }
    }
}