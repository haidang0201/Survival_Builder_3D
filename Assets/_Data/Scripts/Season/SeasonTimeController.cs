using UnityEngine;

public class SeasonTimeController : MonoBehaviour
{
    [Header("Cài đặt Random Thời gian (Giây)")]
    [Tooltip("Thời gian ngắn nhất một mùa có thể kéo dài")]
    public float minDuration = 60f;

    [Tooltip("Thời gian dài nhất một mùa có thể kéo dài")]
    public float maxDuration = 180f;

    [Header("Thông tin đếm ngược (Chỉ để xem)")]
    public float timeRemaining;
    public SeasonType nextSeason;

    private void Start()
    {
        // Bắt đầu game luôn là mùa Xuân
        ChuyenSangMua(SeasonType.Xuan);
    }

    private void Update()
    {
        // Đếm ngược thời gian
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
        }
        else
        {
            // Hết giờ -> Chuyển sang mùa tiếp theo đã được lên lịch
            ChuyenSangMua(nextSeason);
        }
    }

    private void ChuyenSangMua(SeasonType muaMoi)
    {
        // 1. Random ra một khoảng thời gian mới cho mùa này
        timeRemaining = Random.Range(minDuration, maxDuration);

        // 2. Báo cho SeasonManager phát thông báo đổi màu cây, nhạc, vfx...
        if (SeasonManager.Instance != null)
        {
            SeasonManager.Instance.SetSeason(muaMoi);
        }

        // 3. Lên lịch cho mùa tiếp theo (Theo đúng thứ tự: Xuân -> Mưa -> Hè -> Lạnh)
        nextSeason = TinhMuaTiepTheo(muaMoi);

        Debug.Log($"<color=cyan>[Hệ Thống Mùa]</color> Đã sang {muaMoi}. Kéo dài {Mathf.RoundToInt(timeRemaining)}s. Mùa tới sẽ là: {nextSeason}");
    }

    // Thuật toán gán vòng lặp thứ tự mùa
    private SeasonType TinhMuaTiepTheo(SeasonType muaHienTai)
    {
        switch (muaHienTai)
        {
            case SeasonType.Xuan: return SeasonType.Thu;  // Xuân xong tới Mưa
            case SeasonType.Thu: return SeasonType.He;   // Mưa xong tới Hè
            case SeasonType.He: return SeasonType.Lanh; // Hè xong tới Lạnh (Đông)
            case SeasonType.Lanh: return SeasonType.Xuan; // Lạnh xong quay lại Xuân
            default: return SeasonType.Xuan;
        }
    }
}