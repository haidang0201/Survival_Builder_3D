using UnityEngine;

public class SeasonTimeController : MonoBehaviour
{
    [Header("Cài đặt Random Thời gian (Giây)")]
    public float minDuration = 60f;
    public float maxDuration = 180f;

    [Header("Thông tin đếm ngược")]
    public float timeRemaining;
    public SeasonType nextSeason;

    private void Start()
    {
        ChuyenSangMua(SeasonType.Xuan);
    }

    private void Update()
    {
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
        }
        else
        {
            ChuyenSangMua(nextSeason);
        }
    }

    private void ChuyenSangMua(SeasonType muaMoi)
    {
        timeRemaining = Random.Range(minDuration, maxDuration);

        if (SeasonManager.Instance != null)
        {
            SeasonManager.Instance.SetSeason(muaMoi);
        }

        nextSeason = TinhMuaTiepTheo(muaMoi);
    }

    // Thứ tự chuyển mùa mới (Có thêm mùa Thu)
    private SeasonType TinhMuaTiepTheo(SeasonType muaHienTai)
    {
        switch (muaHienTai)
        {
            case SeasonType.Xuan: return SeasonType.He;   // Xuân xong tới Hè
            case SeasonType.He: return SeasonType.Thu;  // Hè xong tới Thu
            case SeasonType.Thu: return SeasonType.Mua;  // Thu xong tới Mưa
            case SeasonType.Mua: return SeasonType.Dong; // Mưa xong tới Đông
            case SeasonType.Dong: return SeasonType.Xuan; // Đông xong về Xuân
            default: return SeasonType.Xuan;
        }
    }
}