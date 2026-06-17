using UnityEngine;
using TMPro;

public class QuestManager : MonoBehaviour
{
    public TextMeshProUGUI questText;

    public void SetQuest(string questDescription)
    {
        if (questText != null)
            questText.text = questDescription;

        Debug.Log("New Quest: " + questDescription);
    }
}