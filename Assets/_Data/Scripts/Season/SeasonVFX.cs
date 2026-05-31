using UnityEngine;

public class SeasonVFX : MonoBehaviour
{
    [Header("Kéo các hệ thống Particle (VFX) vào đây")]
    public GameObject vfxXuan;  // Ví dụ: Lá bay, bồ công anh
    public GameObject vfxMua;   // Hạt mưa
    public GameObject vfxHe;    // Ví dụ: Đom đóm, bụi nắng, tia sáng mặt trời
    public GameObject vfxTuyet; // Hạt tuyết (Lạnh)

    private void OnEnable()
    {
        SeasonManager.OnSeasonChanged += DoiHieuUngThoiTiet;
    }

    private void OnDisable()
    {
        SeasonManager.OnSeasonChanged -= DoiHieuUngThoiTiet;
    }

    private void DoiHieuUngThoiTiet(SeasonType muaMoi)
    {
        // 1. Tắt hết mọi hiệu ứng cũ trước khi bật hiệu ứng mới
        if (vfxXuan != null) vfxXuan.SetActive(false);
        if (vfxMua != null) vfxMua.SetActive(false);
        if (vfxHe != null) vfxHe.SetActive(false);
        if (vfxTuyet != null) vfxTuyet.SetActive(false);

        // 2. Bật hiệu ứng tương ứng với mùa hiện tại
        switch (muaMoi)
        {
            case SeasonType.Xuan:
                if (vfxXuan != null) vfxXuan.SetActive(true);
                break;
            case SeasonType.Thu:
                if (vfxMua != null) vfxMua.SetActive(true);
                break;
            case SeasonType.He:
                if (vfxHe != null) vfxHe.SetActive(true);
                break;
            case SeasonType.Lanh:
                if (vfxTuyet != null) vfxTuyet.SetActive(true);
                break;
        }
    }
}