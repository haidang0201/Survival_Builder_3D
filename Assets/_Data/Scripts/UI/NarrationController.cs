using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class NarrationController : MonoBehaviour
{
    public enum TransitionType
    {
        Instant,
        CrossFade,
        FadeThroughBlack,
        ScreenShakeOnly,
        RedFlashOnly,
        ShakeAndRedFlash
    }

    [Header("UI Components (2D Canvas)")]
    public Canvas targetCanvas;
    public Image backgroundImage;
    public Image fadeImageOverlay;
    public TextMeshProUGUI narrationText;

    [Header("Auto & Skip Settings")]
    public Button autoButton;            
    public Button skipButton;            
    public float autoDelay = 2.0f;       
    public bool autoCreateTopRightUI = true; // Tự tạo UI ở GÓC TRÊN BÊN PHẢI

    [Header("3D Camera Component")]
    public Camera mainCamera;            

    [Header("Audio Components")]
    public AudioSource audioSource;

    [Header("Settings")]
    public float typingSpeed = 0.05f;
    public float defaultFadeDuration = 0.8f;
    public int nextSceneIndex = 2;       

    [System.Serializable]
    public class NarrationStep
    {
        public Sprite backgroundSprite;
        public TransitionType transition;
        [TextArea(3, 5)]
        public string textContent;
        public AudioClip voiceOrSFX;
    }

    [Header("Story Timeline")]
    public NarrationStep[] storySteps;

    private int currentIndex = 0;
    private Coroutine typingCoroutine;
    private Coroutine transitionCoroutine;
    private Coroutine autoRoutine;
    private Coroutine glowRoutine; 
    private bool isTyping = false;
    private bool isAuto = false;

    private Vector3 originalCamPos;
    private TextMeshProUGUI autoBtnText;

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

        // TỰ ĐỘNG TẠO UI BÌNH THƯỜNG Ở GÓC TRÊN BÊN PHẢI
        if (autoCreateTopRightUI && (autoButton == null || skipButton == null))
        {
            CreateTopRightButtons();
        }

        if (autoButton != null)
        {
            autoButton.onClick.AddListener(ToggleAuto);
            autoBtnText = autoButton.GetComponentInChildren<TextMeshProUGUI>();
            UpdateAutoButtonUI();
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(SkipNarration);
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
            if (autoRoutine != null) StopCoroutine(autoRoutine);
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
            CheckAutoNext();
        }
        else
        {
            AdvanceToNextStep();
        }
    }

    private void AdvanceToNextStep()
    {
        currentIndex++;
        if (currentIndex < storySteps.Length)
        {
            PlayStep(currentIndex);
        }
        else
        {
            FinishNarration();
        }
    }

    // ================= CHẾ ĐỘ AUTO =================
    public void ToggleAuto()
    {
        isAuto = !isAuto;
        UpdateAutoButtonUI();

        if (glowRoutine != null) StopCoroutine(glowRoutine);
        if (isAuto)
        {
            glowRoutine = StartCoroutine(AutoGlowRoutine());
            if (!isTyping) CheckAutoNext();
        }
        else
        {
            ResetAutoGlowVisuals();
            if (autoRoutine != null) StopCoroutine(autoRoutine);
        }
    }

    private void UpdateAutoButtonUI()
    {
        if (autoBtnText != null)
        {
            autoBtnText.text = isAuto ? "AUTO ON" : "AUTO";
        }
    }

    // Hiệu ứng đổi màu nhẹ chữ AUTO khi đang kích hoạt
    IEnumerator AutoGlowRoutine()
    {
        float timer = 0f;
        Color goldColor = new Color(1f, 0.85f, 0.3f, 1f); 
        Color brightWhite = Color.white;

        while (isAuto)
        {
            timer += Time.deltaTime * 4f;
            float pulse = (Mathf.Sin(timer) + 1f) / 2f;

            Color currentGlow = Color.Lerp(goldColor, brightWhite, pulse);
            if (autoBtnText != null) autoBtnText.color = currentGlow;

            yield return null;
        }

        ResetAutoGlowVisuals();
    }

    private void ResetAutoGlowVisuals()
    {
        if (autoBtnText != null) autoBtnText.color = Color.white;
    }

    private void CheckAutoNext()
    {
        if (!isAuto) return;
        if (autoRoutine != null) StopCoroutine(autoRoutine);
        autoRoutine = StartCoroutine(AutoNextRoutine());
    }

    IEnumerator AutoNextRoutine()
    {
        yield return new WaitForSeconds(autoDelay);
        if (isAuto) AdvanceToNextStep();
    }

    // ================= XỬ LÝ NÚT SKIP =================
    public void SkipNarration()
    {
        StopAllCoroutines();

        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();

        if (mainCamera != null) mainCamera.transform.localPosition = originalCamPos;

        if (fadeImageOverlay != null)
        {
            Color c = fadeImageOverlay.color;
            c.a = 0f;
            fadeImageOverlay.color = c;
        }

        FinishNarration();
    }

    private void FinishNarration()
    {
        narrationText.text = "";
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
        SceneManager.LoadScene(nextSceneIndex);
    }

    void PlayStep(int index)
    {
        if (autoRoutine != null) StopCoroutine(autoRoutine);

        TransitionType currentTransition = storySteps[index].transition;

        if (storySteps[index].backgroundSprite != null &&
            currentTransition != TransitionType.ScreenShakeOnly &&
            currentTransition != TransitionType.RedFlashOnly)
        {
            if (backgroundImage.sprite == null || currentTransition == TransitionType.Instant)
            {
                backgroundImage.sprite = storySteps[index].backgroundSprite;
            }
        }

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
                transitionCoroutine = StartCoroutine(ScreenShakeRoutine(0.5f, 0.2f));
                break;
            case TransitionType.RedFlashOnly:
                transitionCoroutine = StartCoroutine(RedFlashRoutine(0.4f));
                break;
            case TransitionType.ShakeAndRedFlash:
                backgroundImage.sprite = storySteps[index].backgroundSprite;
                StartCoroutine(ScreenShakeRoutine(0.6f, 0.3f));
                transitionCoroutine = StartCoroutine(RedFlashRoutine(0.5f));
                break;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
            if (storySteps[index].voiceOrSFX != null)
            {
                audioSource.clip = storySteps[index].voiceOrSFX;
                audioSource.Play();
            }
        }

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeNarration(storySteps[index].textContent));
    }

    // ================= TỰ TẠO CỤM NÚT UI CHUẨN Ở GÓC TRÊN BÊN PHẢI =================
    private void CreateTopRightButtons()
    {
        if (targetCanvas == null) targetCanvas = FindObjectOfType<Canvas>();
        if (targetCanvas == null) return;

        // 1. Panel chứa cụm nút bám góc trên bên phải
        GameObject panelObj = new GameObject("TopRight_ControlPanel", typeof(RectTransform));
        panelObj.transform.SetParent(targetCanvas.transform, false);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(1, 1);
        panelRect.anchoredPosition = new Vector2(-20, -20);
        panelRect.sizeDelta = new Vector2(250, 44);

        HorizontalLayoutGroup hGroup = panelObj.AddComponent<HorizontalLayoutGroup>();
        hGroup.spacing = 10;
        hGroup.childControlWidth = true;
        hGroup.childControlHeight = true;
        hGroup.childForceExpandWidth = false;

        // 2. Tạo Nút AUTO
        if (autoButton == null)
        {
            autoButton = CreateButton(panelObj.transform, "AutoButton", "AUTO", new Color(0.12f, 0.12f, 0.12f, 0.85f));
            autoButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 115;
        }

        // 3. Tạo Nút SKIP
        if (skipButton == null)
        {
            skipButton = CreateButton(panelObj.transform, "SkipButton", "SKIP >>", new Color(0.12f, 0.12f, 0.12f, 0.85f));
            skipButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 115;
        }
    }

    private Button CreateButton(Transform parent, string name, string labelText, Color bgColor)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        Image img = btnObj.GetComponent<Image>();
        img.color = bgColor;

        Button btn = btnObj.GetComponent<Button>();

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = labelText;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 16;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.enableWordWrapping = false; // Tắt tự động xuống dòng

        return btn;
    }

    // ================= KHU VỰC HÀM HIỆU ỨNG NỀN & CHỮ =================

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
        CheckAutoNext();
    }

    IEnumerator ScreenShakeRoutine(float duration, float magnitude)
    {
        if (mainCamera == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            mainCamera.transform.localPosition = new Vector3(originalCamPos.x + x, originalCamPos.y + y, originalCamPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        mainCamera.transform.localPosition = originalCamPos;
    }

    IEnumerator RedFlashRoutine(float duration)
    {
        if (fadeImageOverlay == null) yield break;
        fadeImageOverlay.sprite = null;
        fadeImageOverlay.color = Color.red;
        float halfDuration = duration / 2f;
        float timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            Color c = fadeImageOverlay.color;
            c.a = Mathf.Lerp(0f, 0.6f, timer / halfDuration);
            fadeImageOverlay.color = c;
            yield return null;
        }
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
}