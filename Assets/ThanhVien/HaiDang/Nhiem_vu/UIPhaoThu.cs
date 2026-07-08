using UnityEngine;

public class UIPhaoThu : MonoBehaviour
{
    public static UIPhaoThu Instance;

    [Header("Tag")]
    public string cannonTag = "Cannon";

    public int cannonCount;


    void Awake()
    {
        Instance = this;
    }


    void Update()
    {
        cannonCount = CountCannons();
    }


    public int GetCannonCount()
    {
        return cannonCount;
    }


    public int CountCannons()
    {
        GameObject[] cannons =
            GameObject.FindGameObjectsWithTag(cannonTag);

        return cannons.Length;
    }
}