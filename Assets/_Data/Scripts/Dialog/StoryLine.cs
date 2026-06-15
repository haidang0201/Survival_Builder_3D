// StoryLine.cs
using UnityEngine;

[CreateAssetMenu(fileName = "StoryLine", menuName = "Story/StoryLine")]
public class StoryLineData : ScriptableObject
{
    [TextArea] public string speakerName;
    [TextArea(2, 5)] public string title;
    [TextArea(3, 8)] public string content;
    public Sprite portrait;
}