using UnityEngine;

public class SoldierTraining : MonoBehaviour
{
    [Header("References")]
    public Animator     animator;
    public AudioSource  audioSource;

    [Header("Sound Clips")]
    public AudioClip swingSound;
    public AudioClip impactSound;

    [Header("Timing")]
    [Tooltip("Tên state Attack trong Animator (Base Layer)")]
    public string attackStateName   = "Attack";

    [Tooltip("% animation khi tiếng kiếm bắt đầu (~frame 20/47 ≈ 0.43)")]
    [Range(0f, 1f)]
    public float swingSoundRatio    = 0.43f;

    [Tooltip("% animation khi kiếm đạt đỉnh chém (~frame 24/47 ≈ 0.51)")]
    [Range(0f, 1f)]
    public float impactSoundRatio   = 0.51f;

    [Tooltip("Pitch ngẫu nhiên ±range để tránh âm thanh đơn điệu khi loop")]
    [Range(0f, 0.2f)]
    public float pitchVariance      = 0.08f;

    // ─── private state ────────────────────────────────────────────────────────
    private bool hasPlayedSwingSound  = false;
    private bool hasPlayedImpactSound = false;

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (animator    == null) animator    = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        bool inAttack = info.IsName(attackStateName);

        if (!inAttack)
        {
            hasPlayedSwingSound  = false;
            hasPlayedImpactSound = false;
            return;
        }

        float t = info.normalizedTime % 1f;

        if (!hasPlayedSwingSound && t >= swingSoundRatio)
        {
            hasPlayedSwingSound = true;
            PlaySound(swingSound);
        }

        if (!hasPlayedImpactSound && t >= impactSoundRatio)
        {
            hasPlayedImpactSound = true;
            PlaySound(impactSound);
        }

        if (t < swingSoundRatio)
        {
            hasPlayedSwingSound  = false;
            hasPlayedImpactSound = false;
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;

        // Dùng clip + Play() thay vì PlayOneShot
        // → AudioSource 3D spatial settings (Max Distance, Spatial Blend) mới có tác dụng
        audioSource.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
        audioSource.clip  = clip;
        audioSource.Play();
    }
}