using UnityEngine;
using System.Collections.Generic;

public class StoryManager : MonoBehaviour
{
    public List<StoryStep> storySequence; // Kéo các file ScriptableObject ở Bước 1 vào đây
    public DialogueManager dialogueManager;

    private int currentStepIndex = 0;

    private void OnEnable()
    {
        // Đăng ký nhận sự kiện từ DialogueManager
        dialogueManager.OnDialogueFinished += LoadNextStep;
    }

    private void OnDisable()
    {
        dialogueManager.OnDialogueFinished -= LoadNextStep;
    }

    // Gọi hàm này khi bắt đầu tương tác NPC (Trigger)
    public void StartStory()
    {
        currentStepIndex = 0;
        LoadCurrentStep();
    }

    private void LoadCurrentStep()
    {
        if (currentStepIndex < storySequence.Count)
        {
            dialogueManager.ShowDialogue(storySequence[currentStepIndex]);
        }
        else
        {
            Debug.Log("Hết cốt truyện, chuyển sang Main Game Loop");
            // Kích hoạt hệ thống nhiệm vụ tại đây
        }
    }

    private void LoadNextStep()
    {
        currentStepIndex++;
        LoadCurrentStep();
    }
}