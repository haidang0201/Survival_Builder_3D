using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class NightEffectsController : MonoBehaviour
{
    [Header("Hiệu ứng Âm thanh (Ban đêm)")]
    public AudioClip eerieSound;
    private AudioSource audioSource;

    [Header("Hiệu ứng Hình ảnh (Ban đêm)")]
    public GameObject firefliesVFX;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }

    // ĐÃ CHUYỂN TỪ OnEnable SANG Start ĐỂ FIX LỖI MẤT HIỆU ỨNG TỐI
    private void Start()
    {
        if (DayNightManager.Ins != null)
        {
            // Đăng ký nhận thông báo chuyển ngày/đêm
            DayNightManager.Ins.OnDayStart += StopNightEffects;
            DayNightManager.Ins.OnNightStart += PlayNightEffects;

            // Kiểm tra ngay lúc vừa vào game để bật/tắt cho đúng
            if (DayNightManager.Ins.IsNight())
            {
                PlayNightEffects();
            }
            else
            {
                StopNightEffects();
            }
        }
    }

    // ĐỔI OnDisable THÀNH OnDestroy ĐỂ DỌN DẸP SẠCH SẼ
    private void OnDestroy()
    {
        if (DayNightManager.Ins != null)
        {
            DayNightManager.Ins.OnDayStart -= StopNightEffects;
            DayNightManager.Ins.OnNightStart -= PlayNightEffects;
        }
    }

    private void PlayNightEffects()
    {
        // 1. Bật đom đóm
        if (firefliesVFX != null)
        {
            firefliesVFX.SetActive(true);
            ParticleSystem ps = firefliesVFX.GetComponentInChildren<ParticleSystem>();
            if (ps != null) ps.Play();
        }

        // 2. Bật âm thanh man rợ
        if (eerieSound != null && audioSource != null)
        {
            audioSource.clip = eerieSound;
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
    }

    private void StopNightEffects()
    {
        // 1. Tắt đom đóm
        if (firefliesVFX != null)
        {
            firefliesVFX.SetActive(false);
        }

        // 2. Tắt âm thanh man rợ
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}