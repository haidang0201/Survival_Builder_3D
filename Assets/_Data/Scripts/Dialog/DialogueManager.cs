using UnityEngine;
using TMPro;
using System;

public class DialogueManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI txtName;
    public TextMeshProUGUI txtDialogue;

    // Sự kiện báo cho StoryManager biết đã đọc xong đoạn thoại
    public event Action OnDialogueFinished;

    void Start() => dialoguePanel.SetActive(false);

    public void ShowDialogue(StoryStep step)
    {
        dialoguePanel.SetActive(true);
        txtName.text = step.npcName;
        txtDialogue.text = step.dialogueText;
    }

    // Gán hàm này vào nút "Tiếp tục" trong UI
    public void OnNextButtonClicked()
    {
        dialoguePanel.SetActive(false);
        OnDialogueFinished?.Invoke(); // Gọi sự kiện
    }
}