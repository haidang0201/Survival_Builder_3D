using System;
using UnityEngine;

public class GameEventSystem : MonoBehaviour
{
    public static GameEventSystem Instance;

    public Action<Transform> OnEnemySpawn;

    void Awake()
    {
        Instance = this;
    }

    public void TriggerEnemySpawn(Transform enemy)
    {
        OnEnemySpawn?.Invoke(enemy);
    }
}