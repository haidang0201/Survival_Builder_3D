using UnityEngine;

[CreateAssetMenu(fileName = "NewStoryStep", menuName = "Story/StoryStep")]
public class StoryStep : ScriptableObject
{
    public string npcName;
    [TextArea(3, 10)] // Giúp khung nhập liệu trong Inspector rộng rãi
    public string dialogueText;
    public bool isTutorialQuest; // Đánh dấu đây là bước mở khóa nhiệm vụ
}