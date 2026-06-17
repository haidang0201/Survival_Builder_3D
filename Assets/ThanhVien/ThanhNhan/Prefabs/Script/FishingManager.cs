using UnityEngine;

public class FishingManager : MonoBehaviour
{
    [Header("Animators")]
    public Animator characterAnimator; // Kéo Animator nhân vật vào đây
    public Animator poleAnimator;      // Kéo Animator cần câu vào đây

    private static readonly int StartFishingTrigger = Animator.StringToHash("StartFishing");
    private static readonly int FishBitingTrigger = Animator.StringToHash("FishBiting");
    private static readonly int CatchFishTrigger = Animator.StringToHash("CatchFish");

    // Hàm an toàn để kiểm tra xem đã kéo thả Animator chưa trước khi chạy code
    private bool IsAnimatorValid()
    {
        if (characterAnimator == null || poleAnimator == null)
        {
            Debug.LogWarning("FishingManager: Thừa hoặc thiếu Animator ở bảng Inspector!");
            return false;
        }
        return true;
    }

    // 1. Gọi khi nhân vật đang ngồi bình thường và bấm nút "Bắt đầu câu"
    public void StartFishing()
    {
        if (!IsAnimatorValid()) return;

        TriggerBoth(StartFishingTrigger);
    }

    // 2. Gọi khi cá rỉa mồi
    public void OnFishBiting()
    {
        if (!IsAnimatorValid()) return;

        TriggerBoth(FishBitingTrigger);
    }

    // 3. Gọi khi người chơi bấm nút giật cần kéo cá lên
    public void CatchFish()
    {
        if (!IsAnimatorValid()) return;

        TriggerBoth(CatchFishTrigger);
    }

    private void TriggerBoth(int triggerHash)
    {
        characterAnimator.ResetTrigger(triggerHash);
        poleAnimator.ResetTrigger(triggerHash);

        characterAnimator.SetTrigger(triggerHash);
        poleAnimator.SetTrigger(triggerHash);
    }
}