using UnityEngine;
using UnityEngine.InputSystem;

public class UITestSimulator : MonoBehaviour
{
    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.digit1Key.wasPressedThisFrame)
            JsonDataManager.Ins.SetGold(JsonDataManager.Ins.gold + 10);

        if (keyboard.digit2Key.wasPressedThisFrame)
            JsonDataManager.Ins.SetGold(Mathf.Max(0, JsonDataManager.Ins.gold - 10));

        if (keyboard.digit3Key.wasPressedThisFrame)
            JsonDataManager.Ins.SetWood(JsonDataManager.Ins.wood + 5);

        if (keyboard.digit4Key.wasPressedThisFrame)
            JsonDataManager.Ins.SetWood(Mathf.Max(0, JsonDataManager.Ins.wood - 5));

        if (keyboard.digit5Key.wasPressedThisFrame)
            JsonDataManager.Ins.SetHP(JsonDataManager.Ins.hp - 0.1f);

        if (keyboard.digit6Key.wasPressedThisFrame)
            JsonDataManager.Ins.SetHP(JsonDataManager.Ins.hp + 0.1f);
    }
}