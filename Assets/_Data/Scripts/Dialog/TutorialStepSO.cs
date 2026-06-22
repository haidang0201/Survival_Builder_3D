using UnityEngine;

[CreateAssetMenu(fileName = "TutorialStep",
                 menuName = "Tutorial/TutorialStep")]
public class TutorialStepSO : ScriptableObject
{
    [Header("Nội dung dialog Phó lý nói")]
    [TextArea(3, 6)]
    public string dialogContent;

    [Header("Icon cần highlight + click (để trống nếu không cần)")]
    [Tooltip("Điền đúng tên GameObject icon trong HUD: WoodIcon / StoneIcon / FoodIcon...")]
    public string iconName;

    [Header("Nhiệm vụ mở ra sau step này (để trống nếu không có)")]
    [TextArea(2, 3)]
    public string questDescription;
}