using UnityEngine;
using UnityEngine.UI;

/*
 * SoundSettingsUI.cs
 * Thư mục: Assets/ThanhVien/NhatTien/Script/Sound/
 * Tác giả: Nhật Tiến
 * 
 * CHỨC NĂNG:
 * Gắn vào Panel Settings/Âm Thanh trong Canvas UI.
 * Tự động kết nối các Slider & Toggle với AudioManager mà không cần viết code sự kiện OnValueChanged bằng tay trong Inspector.
 */

public class SoundSettingsUI : MonoBehaviour
{
    [Header("Sliders Âm lượng")]
    public Slider masterVolumeSlider;
    public Slider bgmVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider uiVolumeSlider;

    [Header("Toggles Mute (Tùy chọn)")]
    public Toggle masterMuteToggle;
    public Toggle bgmMuteToggle;
    public Toggle sfxMuteToggle;
    public Toggle uiMuteToggle;

    void OnEnable()
    {
        SetupUI();
    }

    void Start()
    {
        SetupUI();
    }

    /// <summary>
    /// Đồng bộ giá trị lưu trong AudioManager lên UI và đăng ký sự kiện lắng nghe.
    /// </summary>
    public void SetupUI()
    {
        if (AudioManager.Instance == null) return;

        // Tự động bind giá trị & sự kiện đổi volume
        AudioManager.Instance.BindSettingControls(
            masterVolumeSlider, 
            bgmVolumeSlider, 
            sfxVolumeSlider, 
            uiVolumeSlider
        );

        // Tự động bind trạng thái Mute
        AudioManager.Instance.BindSettingToggles(
            masterMuteToggle, 
            bgmMuteToggle, 
            sfxMuteToggle, 
            uiMuteToggle
        );
    }
}
