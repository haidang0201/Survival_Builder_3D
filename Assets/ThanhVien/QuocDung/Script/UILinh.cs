using UnityEngine;
using TMPro;

public class UILinh : MonoBehaviour
{
    public static UILinh Instance;

    [Header("UI Reference")]
    public TMP_Text textCount;

    [Header("Settings")]
    public string soldierTag = "Soldier";

    public int soldierCount;


    void Awake()
    {
        Instance = this;
    }


    void Update()
    {
        soldierCount = CountSoldiers();

        if (textCount != null)
            textCount.text = soldierCount.ToString();
    }


    public int GetSoldierCount()
    {
        return soldierCount;
    }


    public int CountSoldiers()
    {
        GameObject[] soldiers = GameObject.FindGameObjectsWithTag(soldierTag);

        return soldiers.Length;
    }
}