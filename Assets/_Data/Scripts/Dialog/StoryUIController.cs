// StoryUIController.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
public class StoryUIController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject storyPanel;
    public Image portraitImage;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public Button continueButton;
    public Button skipButton;
    [Header("Story Data")]
    public StoryLineData[] storyLines;
    [Header("Typing Effect")]
    public float typingSpeed = 0.03f;
    [Header("Events")]
    public UnityEvent onStoryFinished; // gắn GameManager.StartMainGame() vào đây
    [Header("Portrait Animation")]
    [Tooltip("Thời gian crossfade (giây) khi đổi sang sprite portrait khác")]
    public float portraitCrossfadeDuration = 0.25f;
    [Tooltip("Biên độ phóng to khi bounce (1.08 = phóng to 8%)")]
    public float portraitBounceScale = 1.08f;
    [Tooltip("Thời gian thực hiện hiệu ứng bounce (giây)")]
    public float portraitBounceDuration = 0.3f;
    [Tooltip("Góc nghiêng đầu tối đa (độ)")]
    public float portraitTiltAngle = 6f;
    [Tooltip("Thời gian thực hiện hiệu ứng nghiêng đầu (giây)")]
    public float portraitTiltDuration = 0.35f;

    private Sprite lastPortraitSprite;
    private Coroutine portraitAnimCoroutine;
    private Vector3 portraitOriginalScale = Vector3.one;
    private Quaternion portraitOriginalRotation = Quaternion.identity;
    private Color portraitOriginalColor = Color.white;
    private bool portraitScaleInitialized = false;

    // 8 nhóm biểu cảm theo đúng bảng bạn cung cấp. Vì hiện chỉ có 1 sprite portrait (chưa có
    // ảnh biểu cảm riêng cho từng dòng), mỗi nhóm được giả lập bằng tổ hợp ANIMATION (tốc độ,
    // biên độ, kiểu chuyển động) + TINT MÀU đặc trưng, thay vì đổi ảnh. Khi sau này có sprite
    // biểu cảm thật, chỉ cần gán vào StoryLineData.portrait, code sẽ tự crossfade — phần map
    // emotion bên dưới vẫn áp dụng song song để tăng thêm cảm xúc.
    // FIX: enum phải là "public" (không phải "private") vì field emotionMap bên dưới là public
    // và dùng PortraitEmotion làm kiểu phần tử. Nếu enum private trong khi field public, C#
    // báo lỗi CS0050 "Inconsistent accessibility" — đây chính là gạch đỏ bạn gặp trong Editor.
    public enum PortraitEmotion { Thoughtful, Sad, Warm, Tense, Determined, Proud, Talk, Idle }

    // Map CHÍNH XÁC theo bảng 18 StoryLine bạn gửi (index 0 = StoryLine_01 ... index 16 = StoryLine_17).
    // Sửa trực tiếp mảng này trong Inspector nếu cần tinh chỉnh từng dòng mà không phải sửa code.
    [Header("Portrait Emotion Map (theo thứ tự StoryLine)")]
    [Tooltip("Biểu cảm tương ứng cho từng StoryLine theo đúng thứ tự trong mảng storyLines. Nếu để thiếu, các dòng còn lại sẽ dùng Idle.")]
    public PortraitEmotion[] emotionMap = new PortraitEmotion[]
    {
        PortraitEmotion.Thoughtful, // 01 - Đang hồi tưởng, nhìn xa
        PortraitEmotion.Sad,        // 02 - Kể về làng bị tàn phá
        PortraitEmotion.Thoughtful, // 03 - Khoảnh khắc nhân vật chính thay đổi
        PortraitEmotion.Warm,       // 04 - Dân làng bắt đầu tin, ấm áp
        PortraitEmotion.Warm,       // 05 - Làng có lại tiếng cười
        PortraitEmotion.Tense,      // 06 - Bọn cướp quay lại, căng thẳng
        PortraitEmotion.Determined, // 07 - Ánh mắt quyết tâm
        PortraitEmotion.Proud,      // 08 - Trưởng làng trao quyền, trang trọng
        PortraitEmotion.Warm,       // 09 - Phó làng tự giới thiệu (Warm + Talk)
        PortraitEmotion.Talk,       // 10 - Dẫn tham quan, lời kể trực tiếp
        PortraitEmotion.Talk,       // 11 - Chỉ UI, nói chuyện với người chơi
        PortraitEmotion.Talk,       // 12 - Giải thích worker
        PortraitEmotion.Warm,       // 13 - Nói về hy vọng (Talk + Warm)
        PortraitEmotion.Tense,      // 14 - Nhắc nhở phòng thủ
        PortraitEmotion.Proud,      // 15 - Tự hào giới thiệu hệ thống
        PortraitEmotion.Warm,       // 16 - Giao việc lại, nhẹ nhõm
        PortraitEmotion.Proud,      // 17 - Kết thúc, nụ cười bình thản (Proud + Idle)
    };

    private int currentIndex = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    [SerializeField] private AudioSource typingAudioSource;
    // Bỏ qua âm thanh cho khoảng trắng/dấu câu để đỡ rối
    private static readonly HashSet<char> silentChars = new HashSet<char> { ' ', '\n', '\t' };
#if UNITY_EDITOR
    void OnValidate()
    {
        // Cảnh báo sớm trong Editor: nếu clip dài hơn typingSpeed, âm thanh các ký tự sẽ
        // bị Stop() cắt ngang liên tục => nghe rè/đứt hoặc vẫn có cảm giác trễ nhịp.
        if (typingAudioSource != null && typingAudioSource.clip != null)
        {
            if (typingAudioSource.clip.length > typingSpeed)
            {
                Debug.LogWarning(
                    $"[StoryUIController] Audio clip '{typingAudioSource.clip.name}' dài " +
                    $"{typingAudioSource.clip.length:F3}s nhưng typingSpeed = {typingSpeed:F3}s. " +
                    $"Clip sẽ bị Stop() cắt ngang ở mỗi ký tự, dễ nghe rè/giật. " +
                    $"Nên dùng clip ngắn (vd: tiếng 'tick' đơn, không reverb/tail dài) dưới {typingSpeed:F3}s.",
                    this);
            }
        }
    }
#endif

    void Start()
    {
        continueButton.onClick.AddListener(OnContinue);
        //skipButton.onClick.AddListener(SkipAllStory);
        if (storyLines.Length > 0)
        {
            storyPanel.SetActive(true);
            ShowLine(0);
        }
        else
        {
            EndStory();
        }
    }
    // ===== PORTRAIT ANIMATION (mới thêm, không ảnh hưởng logic gõ chữ/âm thanh phía dưới) =====

    // Mỗi dòng thoại tra cứu đúng 1 PortraitEmotion theo emotionMap[index], rồi chạy animation
    // + tint màu đặc trưng cho cảm xúc đó. Tất cả animation chỉ đổi scale/rotation/color —
    // KHÔNG bao giờ đụng tới vị trí (position/anchoredPosition), nên ảnh portrait LUÔN đứng
    // yên đúng chỗ như trong layout gốc, chỉ "diễn" biểu cảm tại chỗ.
    private void AnimatePortraitChange(Sprite newSprite, PortraitEmotion emotion)
    {
        if (portraitImage == null) return;

        if (!portraitScaleInitialized)
        {
            portraitOriginalScale = portraitImage.rectTransform.localScale;
            portraitOriginalRotation = portraitImage.rectTransform.localRotation;
            portraitOriginalColor = portraitImage.color;
            portraitScaleInitialized = true;
        }

        if (portraitAnimCoroutine != null) StopCoroutine(portraitAnimCoroutine);

        // Luôn reset về trạng thái gốc trước khi chạy animation mới, tránh cộng dồn lệch
        // scale/góc xoay/màu nếu coroutine trước đó bị ngắt giữa chừng (bấm Continue liên tục).
        portraitImage.rectTransform.localScale = portraitOriginalScale;
        portraitImage.rectTransform.localRotation = portraitOriginalRotation;

        bool spriteChanged = newSprite != lastPortraitSprite;
        lastPortraitSprite = newSprite;

        if (newSprite == null)
        {
            portraitImage.enabled = false;
            return;
        }

        portraitImage.enabled = true;

        if (spriteChanged && portraitImage.sprite != null)
        {
            portraitAnimCoroutine = StartCoroutine(CrossfadePortrait(newSprite, emotion));
        }
        else
        {
            portraitImage.sprite = newSprite;
            portraitAnimCoroutine = StartCoroutine(PlayEmotion(emotion));
        }
    }

    // Điều phối: chọn đúng coroutine animation ứng với từng nhóm cảm xúc trong bảng của bạn.
    private IEnumerator PlayEmotion(PortraitEmotion emotion)
    {
        switch (emotion)
        {
            case PortraitEmotion.Thoughtful:
                yield return ThoughtfulAnim();
                break;
            case PortraitEmotion.Sad:
                yield return SadAnim();
                break;
            case PortraitEmotion.Warm:
                yield return WarmAnim();
                break;
            case PortraitEmotion.Tense:
                yield return TenseAnim();
                break;
            case PortraitEmotion.Determined:
                yield return DeterminedAnim();
                break;
            case PortraitEmotion.Proud:
                yield return ProudAnim();
                break;
            case PortraitEmotion.Talk:
                yield return TalkAnim();
                break;
            default: // Idle
                yield return IdleAnim();
                break;
        }
    }

    // Crossfade: fade-out sprite cũ rồi fade-in sprite mới trên cùng 1 Image, sau đó chạy
    // tiếp animation cảm xúc. Chỉ đổi alpha lúc fade, không đụng vị trí/scale.
    private IEnumerator CrossfadePortrait(Sprite newSprite, PortraitEmotion emotion)
    {
        float half = portraitCrossfadeDuration * 0.5f;
        Color c = portraitImage.color;

        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, t / half);
            portraitImage.color = c;
            yield return null;
        }

        portraitImage.sprite = newSprite;
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, t / half);
            portraitImage.color = c;
            yield return null;
        }

        c.a = portraitOriginalColor.a;
        portraitImage.color = c;

        yield return PlayEmotion(emotion);
    }

    // Helper dùng chung: lerp màu portrait sang 1 tint mục tiêu rồi quay về màu gốc, dùng
    // cho các cảm xúc cần đổi sắc thái (Sad, Tense, Determined, Proud). Giữ alpha gốc nguyên.
    private IEnumerator TintTo(Color targetTint, float inDuration, float holdDuration, float outDuration)
    {
        Color baseColor = portraitOriginalColor;
        Color from = portraitImage.color;
        float t = 0f;
        while (t < inDuration)
        {
            t += Time.deltaTime;
            portraitImage.color = Color.Lerp(from, targetTint, t / inDuration);
            yield return null;
        }
        portraitImage.color = targetTint;

        yield return new WaitForSeconds(holdDuration);

        t = 0f;
        Color holdColor = portraitImage.color;
        while (t < outDuration)
        {
            t += Time.deltaTime;
            portraitImage.color = Color.Lerp(holdColor, baseColor, t / outDuration);
            yield return null;
        }
        portraitImage.color = baseColor;
    }

    // --- Thoughtful: đang hồi tưởng, nhìn xa — nghiêng đầu CHẬM, biên độ nhỏ, không tint ---
    private IEnumerator ThoughtfulAnim()
    {
        float t = 0f;
        float duration = portraitTiltDuration * 1.6f; // chậm hơn bình thường, trầm
        float angle = portraitTiltAngle * 0.7f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float n = t / duration;
            float a = Mathf.Sin(n * Mathf.PI) * angle;
            portraitImage.rectTransform.localRotation = portraitOriginalRotation * Quaternion.Euler(0f, 0f, a);
            yield return null;
        }
        portraitImage.rectTransform.localRotation = portraitOriginalRotation;
    }

    // --- Sad: kể chuyện làng bị tàn phá — cúi đầu nhẹ xuống (tilt trục X giả qua scale Y
    // hơi co lại) + tint xám nhẹ, chuyển động chậm rãi, nặng nề ---
    private IEnumerator SadAnim()
    {
        Color sadTint = new Color(0.75f, 0.75f, 0.8f, portraitOriginalColor.a); // xám lạnh nhẹ
        Coroutine tint = StartCoroutine(TintTo(sadTint, 0.3f, 0.5f, 0.4f));

        float t = 0f;
        float duration = 0.6f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float n = t / duration;
            // Cúi xuống nhẹ: scale Y co lại chút rồi về lại, như đầu hơi gục xuống
            float dip = Mathf.Sin(n * Mathf.PI) * 0.04f;
            portraitImage.rectTransform.localScale = portraitOriginalScale - new Vector3(0f, dip, 0f);
            yield return null;
        }
        portraitImage.rectTransform.localScale = portraitOriginalScale;
        yield return tint;
    }

    // --- Warm: ấm áp — bounce nhẹ nhàng + tint vàng cam ấm ---
    private IEnumerator WarmAnim()
    {
        Color warmTint = new Color(1f, 0.95f, 0.85f, portraitOriginalColor.a); // vàng kem ấm nhẹ
        Coroutine tint = StartCoroutine(TintTo(warmTint, 0.25f, 0.45f, 0.4f));
        yield return BounceCore(portraitBounceScale * 0.85f, portraitBounceDuration * 1.1f); // bounce dịu, chậm hơn chút
        yield return tint;
    }

    // --- Tense: căng thẳng — rung nhanh, biên độ nhỏ qua scale (không di chuyển vị trí) +
    // tint đỏ nhạt, nhịp gấp gáp ---
    private IEnumerator TenseAnim()
    {
        Color tenseTint = new Color(1f, 0.88f, 0.85f, portraitOriginalColor.a); // đỏ cam rất nhạt
        Coroutine tint = StartCoroutine(TintTo(tenseTint, 0.12f, 0.3f, 0.25f));

        float t = 0f;
        float duration = 0.3f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float n = t / duration;
            float damped = (1f - n) * (portraitTiltAngle * 0.5f);
            // Rung nhanh qua góc xoay biên độ nhỏ, tần số cao — cảm giác "giật mình"
            float angle = Mathf.Sin(n * Mathf.PI * 14f) * damped;
            portraitImage.rectTransform.localRotation = portraitOriginalRotation * Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }
        portraitImage.rectTransform.localRotation = portraitOriginalRotation;
        yield return tint;
    }

    // --- Determined: quyết tâm — bounce DỨT KHOÁT, nhanh và mạnh hơn, tint sáng/contrast ---
    private IEnumerator DeterminedAnim()
    {
        Color determinedTint = new Color(1.05f, 1.05f, 1.05f, portraitOriginalColor.a); // sáng hơn 1 chút
        Coroutine tint = StartCoroutine(TintTo(determinedTint, 0.1f, 0.35f, 0.3f));
        yield return BounceCore(portraitBounceScale * 1.3f, portraitBounceDuration * 0.7f); // nhanh + mạnh
        yield return tint;
    }

    // --- Proud: tự hào, trang trọng — ngẩng cao (tilt dương dứt khoát) + bounce nhẹ + tint
    // vàng gold sáng ---
    private IEnumerator ProudAnim()
    {
        Color proudTint = new Color(1f, 0.96f, 0.8f, portraitOriginalColor.a); // vàng gold ấm sáng
        Coroutine tint = StartCoroutine(TintTo(proudTint, 0.25f, 0.5f, 0.4f));

        float t = 0f;
        float duration = portraitTiltDuration;
        while (t < duration)
        {
            t += Time.deltaTime;
            float n = t / duration;
            // Ngẩng lên 1 chiều dứt khoát (không đổi chiều ngẫu nhiên như Thoughtful)
            float angle = Mathf.Sin(n * Mathf.PI) * portraitTiltAngle;
            float bounce = Mathf.Sin(n * Mathf.PI) * (portraitBounceScale - 1f) * 0.6f;
            portraitImage.rectTransform.localRotation = portraitOriginalRotation * Quaternion.Euler(0f, 0f, angle);
            portraitImage.rectTransform.localScale = portraitOriginalScale * (1f + bounce);
            yield return null;
        }
        portraitImage.rectTransform.localRotation = portraitOriginalRotation;
        portraitImage.rectTransform.localScale = portraitOriginalScale;
        yield return tint;
    }

    // --- Talk: đang nói chuyện trực tiếp (UI, hướng dẫn) — bounce nhỏ, NHANH, lặp 2 nhịp
    // như đang nhấn nhá khi nói, không tint (giữ tự nhiên) ---
    private IEnumerator TalkAnim()
    {
        for (int i = 0; i < 2; i++)
        {
            yield return BounceCore(portraitBounceScale * 0.6f, portraitBounceDuration * 0.55f);
        }
    }

    // --- Idle: trung tính — bounce rất nhẹ, không tint ---
    private IEnumerator IdleAnim()
    {
        yield return BounceCore(portraitBounceScale * 0.5f, portraitBounceDuration);
    }

    // Core dùng chung cho mọi biến thể bounce: phóng to theo đường cong sin rồi co lại,
    // CHỈ đổi localScale quanh tâm pivot — không bao giờ đụng vị trí.
    private IEnumerator BounceCore(float scaleAmount, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float n = t / duration;
            float bounce = Mathf.Sin(n * Mathf.PI) * (scaleAmount - 1f);
            portraitImage.rectTransform.localScale = portraitOriginalScale * (1f + bounce);
            yield return null;
        }
        portraitImage.rectTransform.localScale = portraitOriginalScale;
    }

    // ===== HẾT PHẦN PORTRAIT ANIMATION =====

    void ShowLine(int index)
    {
        currentIndex = index;
        var line = storyLines[index];
        speakerNameText.text = line.speakerName;

        // Tra cứu biểu cảm theo đúng thứ tự StoryLine (index khớp với emotionMap). Nếu mảng
        // emotionMap ngắn hơn storyLines (thiếu dòng), mặc định dùng Idle cho an toàn.
        PortraitEmotion emotion = (emotionMap != null && index < emotionMap.Length)
            ? emotionMap[index]
            : PortraitEmotion.Idle;
        AnimatePortraitChange(line.portrait, emotion); // animation biểu cảm khi chuyển dòng thoại

        // Chỉ chạy phần Content, không hiện Title nữa (title vẫn còn trong StoryLineData
        // nếu sau này cần dùng cho mục đích khác như log, debug...)
        string fullText = line.content;
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(fullText));
    }
    private IEnumerator TypeText(string content)
    {
        isTyping = true; // FIX: trước đây không set true nên OnContinue() không nhận biết đang gõ chữ

        // FIX QUAN TRỌNG: Trước đây dùng foreach(char c in content) và nối từng ký tự thô —
        // vòng lặp này chạy qua CẢ ký tự của tag rich-text như "<b>", "</b>" (6 ký tự ẩn không
        // hiện trên bảng thoại), khiến âm thanh phát thêm 6 lần thừa không khớp với chữ người
        // chơi thực sự nhìn thấy. Cách xử lý đúng: gán toàn bộ content (có tag) vào text MỘT
        // LẦN để TMP parse tag, sau đó dùng maxVisibleCharacters tăng dần — số đếm này chỉ tính
        // ký tự HIỂN THỊ thật sự, tag bị TMP tự động bỏ qua. Nhờ vậy số lần phát âm thanh luôn
        // khớp chính xác 1-1 với số ký tự xuất hiện trong bảng thoại.
        dialogueText.text = content;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        string parsedText = dialogueText.GetParsedText(); // chuỗi đã bỏ hết tag, đúng những gì hiện trên bảng
        int visibleCount = parsedText.Length;

        // Dùng AudioSettings.dspTime (đồng hồ audio thread) thay vì dựa vào thời điểm coroutine
        // được Unity gọi lại (vốn phụ thuộc frame rate, có thể trễ vài ms mỗi frame). Với
        // typingSpeed nhỏ như 0.04s, sai số tích lũy theo frame là nguyên nhân chính khiến
        // âm thanh "trôi" dần so với chữ dù logic Play() đã đúng.
        double nextDspTime = AudioSettings.dspTime;

        for (int i = 0; i < visibleCount; i++)
        {
            char c = parsedText[i];
            dialogueText.maxVisibleCharacters = i + 1;

            if (!silentChars.Contains(c) && typingAudioSource != null && typingAudioSource.clip != null)
            {
                typingAudioSource.pitch = Random.Range(0.95f, 1.05f); // tránh nghe nhàm/máy móc
                typingAudioSource.Stop();
                // PlayScheduled phát đúng tại mốc thời gian dspTime đã định, chính xác tới
                // từng sample audio — không bị lệch do độ trễ render frame như Play() thường.
                typingAudioSource.PlayScheduled(nextDspTime);
            }
            yield return new WaitForSeconds(typingSpeed);
            nextDspTime = AudioSettings.dspTime; // chốt lại mốc audio thực tế cho ký tự kế tiếp
        }
        isTyping = false; // FIX: đánh dấu đã gõ xong để OnContinue() chuyển sang dòng kế tiếp đúng logic
    }
    void OnContinue()
    {
        // Nếu đang gõ chữ, bấm để hiện full text ngay
        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            // dialogueText.text đã chứa đủ nội dung (gán 1 lần trong TypeText), chỉ cần mở
            // hết maxVisibleCharacters là hiện full bảng thoại ngay lập tức.
            dialogueText.maxVisibleCharacters = int.MaxValue;
            if (typingAudioSource != null) typingAudioSource.Stop(); // FIX: tắt âm thanh đang phát dở khi skip
            isTyping = false;
            return;
        }
        int next = currentIndex + 1;
        if (next < storyLines.Length)
        {
            ShowLine(next);
        }
        else
        {
            EndStory();
        }
    }
    // public void SkipAllStory()
    // {
    //     if (typingCoroutine != null) StopCoroutine(typingCoroutine);
    //     EndStory();
    // }
    void EndStory()
    {
        storyPanel.SetActive(false);
        onStoryFinished?.Invoke();
        SceneLoadToGamePlay(); // FIX: hết 18 story (bấm Tiếp tục ở dòng cuối) thì tự load scene index 3
    }
    public void LoadSceneGamePlay()
    {
        SceneManager.LoadScene(2);
    }
    public void SceneLoadToGamePlay()
    {
        SceneManager.LoadScene(3);
    }
}