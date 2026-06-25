using UnityEngine;
using TMPro;

public class QuestResourceText : MonoBehaviour
{
    [Header("TEXT UI")]
    public TMP_Text contentBodyText;

    void Start()
    {
        ShowResource();
    }

    void ShowResource()
    {
        contentBodyText.text =
            "Mục tiêu tài nguyên:\n\n" +
            "<sprite name=\"wood\"> 200 Gỗ\n" +
            "<sprite name=\"stone\"> 100 Đá\n" +
            "<sprite name=\"food\"> 70 Lúa";
    }
}