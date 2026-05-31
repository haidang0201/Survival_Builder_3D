using UnityEngine;

public class SeasonVisuals : MonoBehaviour
{
    [Header("1. Đổi Màu (Materials)")]
    public Material matXuan;
    public Material matHe;
    public Material matThu;   // Mới thêm cho mùa Thu
    public Material matDong;  // Đã đổi tên thành Đông
    public Material matMua;

    [Header("2. Đổi Hình Dáng (Meshes)")]
    public Mesh meshXuan;
    public Mesh meshHe;
    public Mesh meshThu;      // Mới thêm cho mùa Thu
    public Mesh meshDong;     // Đã đổi tên thành Đông
    //public Mesh meshMua;

    private Renderer meshRenderer;
    private MeshFilter meshFilter;

    private void Awake()
    {
        meshRenderer = GetComponent<Renderer>();
        meshFilter = GetComponent<MeshFilter>(); // Lấy component chứa hình dáng gốc của cây
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
        // 1. Thay lớp sơn (Màu Material)
        if (meshRenderer != null)
        {
            switch (muaMoi)
            {
                case SeasonType.Xuan: if (matXuan != null) meshRenderer.material = matXuan; break;
                case SeasonType.He: if (matHe != null) meshRenderer.material = matHe; break;
                case SeasonType.Thu: if (matThu != null) meshRenderer.material = matThu; break;
                case SeasonType.Dong: if (matDong != null) meshRenderer.material = matDong; break;
                    //case SeasonType.Mua: if (matMua != null) meshRenderer.material = matMua; break;
            }
        }

        // 2. Thay hình khối 3D (Đổi dáng cây)
        if (meshFilter != null)
        {
            switch (muaMoi)
            {
                case SeasonType.Xuan: if (meshXuan != null) meshFilter.mesh = meshXuan; break;
                case SeasonType.He: if (meshHe != null) meshFilter.mesh = meshHe; break;
                case SeasonType.Thu: if (meshThu != null) meshFilter.mesh = meshThu; break;
                case SeasonType.Dong: if (meshDong != null) meshFilter.mesh = meshDong; break;
                    //case SeasonType.Mua: if (meshMua != null) meshFilter.mesh = meshMua; break;
            }
        }
    }
}