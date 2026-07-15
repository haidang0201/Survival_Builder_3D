using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UISoundManager : MonoBehaviour
{
    public static UISoundManager Instance { get; private set; }

    [Header("Penta Dev - Cấu hình Âm thanh Click")]
    [SerializeField] private AudioClip clickSFX; 
    [SerializeField] private AudioSource audioSource;

    [Header("Cài đặt Cao độ ngẫu nhiên")]
    [Range(0.8f, 1.2f)] [SerializeField] private float minPitch = 0.95f;
    [Range(0.8f, 1.2f)] [SerializeField] private float maxPitch = 1.05f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; 
    }

    private void OnEnable()
    {
        // Đăng ký sự kiện: Cứ mỗi lần Scene được load xong là tự động quét Button
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RegisterAllButtonsInActiveScene();
    }

    /// <summary>
    /// Hàm cốt lõi: Tự động tìm sạch bách Button của TẤT CẢ Canvas trong Scene hiện tại
    /// </summary>
    public void RegisterAllButtonsInActiveScene()
    {
        // Lấy tất cả Button trong Scene (bao gồm cả các nút đang bị ẩn/nhà nát/bảng ẩn)
        Button[] allButtons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Button btn in allButtons)
        {
            btn.onClick.RemoveListener(PlayClickSound);
            btn.onClick.AddListener(PlayClickSound);
        }
        
        Debug.Log($"[UISoundManager] Đã tự động gán âm thanh click giọt nước cho {allButtons.Length} Buttons trên toàn bộ các Canvas!");
    }

    public void PlayClickSound()
    {
        if (audioSource != null && clickSFX != null)
        {
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(clickSFX);
        }
    }
}