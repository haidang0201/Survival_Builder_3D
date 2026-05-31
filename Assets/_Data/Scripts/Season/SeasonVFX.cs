using UnityEngine;

public class SeasonVFX : MonoBehaviour
{
    [Header("Kéo các hiệu ứng Particle (VFX) vào đây")]
    public GameObject vfxXuan;
    public GameObject vfxHe;
    public GameObject vfxThu;   // Thêm slot cho mùa Thu (Lá vàng rơi chẳng hạn)
    public GameObject vfxDong;  // Mùa Đông (Tuyết)
    public GameObject vfxMua;   // Mùa Mưa (Hạt mưa)

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
        // 1. Tắt toàn bộ VFX cũ
        if (vfxXuan != null) vfxXuan.SetActive(false);
        if (vfxHe != null) vfxHe.SetActive(false);
        if (vfxThu != null) vfxThu.SetActive(false);
        if (vfxDong != null) vfxDong.SetActive(false);
        if (vfxMua != null) vfxMua.SetActive(false);

        // 2. Bật VFX đúng mùa và ép xả hạt (Play)
        switch (muaMoi)
        {
            case SeasonType.Xuan: KichHoatVFX(vfxXuan); break;
            case SeasonType.He: KichHoatVFX(vfxHe); break;
            case SeasonType.Thu: KichHoatVFX(vfxThu); break;
            case SeasonType.Dong: KichHoatVFX(vfxDong); break;
            case SeasonType.Mua: KichHoatVFX(vfxMua); break; // <-- Chuyển qua mưa là gọi dòng này
        }
    }

    private void KichHoatVFX(GameObject vfx)
    {
        if (vfx != null)
        {
            vfx.SetActive(true);

            // Tìm và Ép Particle xả hạt ngay lập tức
            ParticleSystem ps = vfx.GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
            }
        }
    }
}