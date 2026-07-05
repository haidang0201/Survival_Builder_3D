using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class NarrationController : MonoBehaviour
{
    public enum TransitionType
    {
        Instant,           // Đổi ngay lập tức
        CrossFade,         // Mờ dần đè nhau
        FadeThroughBlack,  // Mờ qua màn hình đen
        ScreenShakeOnly,   // Không đổi ảnh, chỉ RUNG màn hình
        RedFlashOnly,      // Không đổi ảnh, chỉ NHÁY ĐỎ cảnh báo
        ShakeAndRedFlash   // Vừa ĐỔI ẢNH, vừa RUNG, vừa NHÁY ĐỎ
    }

    [Header("UI Components (2D Canvas)")]
    public Image backgroundImage;
    public Image fadeImageOverlay;       // Tấm dùng để Fade ảnh hoặc Nháy Đỏ
    public TextMeshProUGUI narrationText;

    [Header("3D Camera Component (Cho hiệu ứng Rung)")]
    public Camera mainCamera;            // Kéo Main Camera của bạn vào đây

    [Header("Audio Components")]
    public AudioSource audioSource;

    [Header("Settings")]
    public float typingSpeed = 0.05f;
    public float defaultFadeDuration = 0.8f;

    [System.Serializable]
    public class NarrationStep
    {
        public Sprite backgroundSprite;
        public TransitionType transition; // Ô chọn hiệu ứng đặc biệt
        [TextArea(3, 5)]
        public string textContent;
        public AudioClip voiceOrSFX;
    }

    [Header("Story Timeline")]
    public NarrationStep[] storySteps;

    private int currentIndex = 0;
    private Coroutine typingCoroutine;
    private Coroutine transitionCoroutine;
    private bool isTyping = false;

    // Lưu vị trí gốc của Camera để sau khi rung không bị lệch camera
    private Vector3 originalCamPos;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null) originalCamPos = mainCamera.transform.localPosition;

        if (fadeImageOverlay != null)
        {
            Color c = fadeImageOverlay.color;
            c.a = 0f;
            fadeImageOverlay.color = c;
        }

        if (storySteps.Length > 0)
        {
            PlayStep(0);
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            OnPlayerClick();
        }
    }

    void OnPlayerClick()
    {
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            narrationText.text = storySteps[currentIndex].textContent;
            isTyping = false;
        }
        else
        {
            currentIndex++;
            if (currentIndex < storySteps.Length)
            {
                PlayStep(currentIndex);
            }
            else
            {
                narrationText.text = "";
                if (audioSource.isPlaying) audioSource.Stop();
                else SceneManager.LoadScene(2);
            }
        }
    }

    void PlayStep(int index)
    {
        // Đọc hiệu ứng được chọn từ Inspector
        TransitionType currentTransition = storySteps[index].transition;

        // Cập nhật ảnh nền trước (nếu có và không thuộc nhóm chỉ hiệu ứng)
        if (storySteps[index].backgroundSprite != null &&
            currentTransition != TransitionType.ScreenShakeOnly &&
            currentTransition != TransitionType.RedFlashOnly)
        {
            if (backgroundImage.sprite == null || currentTransition == TransitionType.Instant)
            {
                backgroundImage.sprite = storySteps[index].backgroundSprite;
            }
        }

        // KÍCH HOẠT CÁC COROUTINE HIỆU ỨNG TRỰC TIẾP
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);

        switch (currentTransition)
        {
            case TransitionType.CrossFade:
                transitionCoroutine = StartCoroutine(CrossFadeRoutine(storySteps[index].backgroundSprite));
                break;
            case TransitionType.FadeThroughBlack:
                transitionCoroutine = StartCoroutine(FadeThroughBlackRoutine(storySteps[index].backgroundSprite));
                break;
            case TransitionType.ScreenShakeOnly:
                transitionCoroutine = StartCoroutine(ScreenShakeRoutine(0.5f, 0.2f)); // Rung 0.5 giây, độ mạnh 0.2
                break;
            case TransitionType.RedFlashOnly:
                transitionCoroutine = StartCoroutine(RedFlashRoutine(0.4f)); // Nháy đỏ trong 0.4 giây
                break;
            case TransitionType.ShakeAndRedFlash:
                // Thay đổi ảnh nền ngay lập tức rồi vừa rung vừa nháy đỏ
                backgroundImage.sprite = storySteps[index].backgroundSprite;
                StartCoroutine(ScreenShakeRoutine(0.6f, 0.3f));
                transitionCoroutine = StartCoroutine(RedFlashRoutine(0.5f));
                break;
        }

        // Âm thanh
        if (audioSource != null)
        {
            audioSource.Stop();
            if (storySteps[index].voiceOrSFX != null)
            {
                audioSource.clip = storySteps[index].voiceOrSFX;
                audioSource.Play();
            }
        }

        // Chạy chữ
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeNarration(storySteps[index].textContent));
    }

    // ================= KHU VỰC CÁC HÀM HIỆU ỨNG =================

    // Hiệu ứng Rung Camera (Dùng được cho cả game 2D và 3D)
    IEnumerator ScreenShakeRoutine(float duration, float magnitude)
    {
        if (mainCamera == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Tính toán vị trí ngẫu nhiên xung quanh vị trí gốc của Camera
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            mainCamera.transform.localPosition = new Vector3(originalCamPos.x + x, originalCamPos.y + y, originalCamPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Trả Camera về vị trí ban đầu sau khi rung xong
        mainCamera.transform.localPosition = originalCamPos;
    }

    // Hiệu ứng Nháy Đỏ Cảnh Báo (Sử dụng tấm Overlay biến thành màu đỏ)
    IEnumerator RedFlashRoutine(float duration)
    {
        if (fadeImageOverlay == null) yield break;

        fadeImageOverlay.sprite = null; // Xóa ảnh trên tấm overlay để nó thành mảng màu thuần
        fadeImageOverlay.color = Color.red;

        float halfDuration = duration / 2f;

        // Pha 1: Đỏ rực lên cực nhanh
        float timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            Color c = fadeImageOverlay.color;
            c.a = Mathf.Lerp(0f, 0.6f, timer / halfDuration); // Đạt độ đậm tối đa là 60% màu đỏ để không bị che mất chữ
            fadeImageOverlay.color = c;
            yield return null;
        }

        // Pha 2: Mờ dần màu đỏ đi
        timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            Color c = fadeImageOverlay.color;
            c.a = Mathf.Lerp(0.6f, 0f, timer / halfDuration);
            fadeImageOverlay.color = c;
            yield return null;
        }

        Color finalOff = fadeImageOverlay.color;
        finalOff.a = 0f;
        fadeImageOverlay.color = finalOff;
    }

    IEnumerator CrossFadeRoutine(Sprite nextSprite)
    {
        if (fadeImageOverlay == null) { backgroundImage.sprite = nextSprite; yield break; }
        fadeImageOverlay.color = Color.white;
        fadeImageOverlay.sprite = nextSprite;
        float timer = 0f;
        while (timer < defaultFadeDuration)
        {
            timer += Time.deltaTime;
            Color c = fadeImageOverlay.color;
            c.a = Mathf.Lerp(0f, 1f, timer / defaultFadeDuration);
            fadeImageOverlay.color = c;
            yield return null;
        }
        backgroundImage.sprite = nextSprite;
        Color finalOff = fadeImageOverlay.color;
        finalOff.a = 0f;
        fadeImageOverlay.color = finalOff;
    }

    IEnumerator FadeThroughBlackRoutine(Sprite nextSprite)
    {
        if (fadeImageOverlay == null) { backgroundImage.sprite = nextSprite; yield break; }
        fadeImageOverlay.sprite = null;
        fadeImageOverlay.color = Color.black;
        float halfDuration = defaultFadeDuration / 2f;
        float timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            Color c = fadeImageOverlay.color;
            c.a = Mathf.Lerp(0f, 1f, timer / halfDuration);
            fadeImageOverlay.color = c;
            yield return null;
        }
        backgroundImage.sprite = nextSprite;
        timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            Color c = fadeImageOverlay.color;
            c.a = Mathf.Lerp(1f, 0f, timer / halfDuration);
            fadeImageOverlay.color = c;
            yield return null;
        }
        Color finalOff = fadeImageOverlay.color;
        finalOff.a = 0f;
        fadeImageOverlay.color = finalOff;
    }

    IEnumerator TypeNarration(string line)
    {
        narrationText.text = "";
        isTyping = true;
        foreach (char letter in line.ToCharArray())
        {
            narrationText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
    }
}