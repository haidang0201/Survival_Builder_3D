using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SeasonAudio : MonoBehaviour
{
    [Header("Nhạc/Âm thanh nền cho 5 mùa")]
    public AudioClip nhacXuan;
    public AudioClip nhacHe;
    public AudioClip nhacThu;   // Mới thêm slot cho mùa Thu
    public AudioClip nhacDong;  // Đã đổi tên từ Lạnh thành Đông
    public AudioClip nhacMua;   // Mùa Mưa

    private AudioSource audioSource;

    private void Awake()
    {
        // Tự động lấy AudioSource trên cùng GameObject
        audioSource = GetComponent<AudioSource>();

        // Đảm bảo nhạc nền sẽ tự động lặp lại khi hết bài
        audioSource.loop = true;
    }

    private void OnEnable()
    {
        // Đăng ký nghe thông báo đổi mùa
        SeasonManager.OnSeasonChanged += DoiAmThanh;
    }

    private void OnDisable()
    {
        // Hủy đăng ký nghe thông báo
        SeasonManager.OnSeasonChanged -= DoiAmThanh;
    }

    private void DoiAmThanh(SeasonType muaMoi)
    {
        // Thay đổi clip nhạc tùy theo mùa mới nhận được
        switch (muaMoi)
        {
            case SeasonType.Xuan:
                audioSource.clip = nhacXuan;
                break;
            case SeasonType.He:
                audioSource.clip = nhacHe;
                break;
            case SeasonType.Thu:
                audioSource.clip = nhacThu;
                break;
            case SeasonType.Dong:
                audioSource.clip = nhacDong;
                break;
            case SeasonType.Mua:
                audioSource.clip = nhacMua;
                break;
        }

        // Phát bài nhạc mới ngay lập tức (nếu đã kéo file vào ô)
        if (audioSource.clip != null)
        {
            audioSource.Play();
        }
    }
}