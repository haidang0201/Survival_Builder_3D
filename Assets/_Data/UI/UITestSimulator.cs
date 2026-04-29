using UnityEngine;
using UnityEngine.InputSystem;

public class UITestSimulator : MonoBehaviour
{
    public HUDController hud;

    private int gold = 100;
    private int wood = 50;
    private float hp = 1f;

    private void Update()
    {
        if (hud == null)
        {
            Debug.LogWarning("HUD chưa được gán!");
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // ===== GOLD =====
        if (keyboard.gKey.wasPressedThisFrame)
        {
            gold += 10;
            hud.UpdateGold(gold);
        }

        if (keyboard.hKey.wasPressedThisFrame)
        {
            gold = Mathf.Max(0, gold - 10);
            hud.UpdateGold(gold);
            hud.Shake();
        }

        // ===== WOOD =====
        if (keyboard.jKey.wasPressedThisFrame)
        {
            wood += 5;
            hud.UpdateWood(wood);
        }

        if (keyboard.uKey.wasPressedThisFrame)
        {
            wood = Mathf.Max(0, wood - 5);
            hud.UpdateWood(wood);
            hud.Shake();
        }

        // ===== HEALTH =====
        if (keyboard.kKey.wasPressedThisFrame)
        {
            hp -= 0.1f;
            hp = Mathf.Clamp01(hp);
            hud.UpdateHealth(hp);
        }

        if (keyboard.lKey.wasPressedThisFrame)
        {
            hp += 0.1f;
            hp = Mathf.Clamp01(hp);
            hud.UpdateHealth(hp);
        }
    }
}