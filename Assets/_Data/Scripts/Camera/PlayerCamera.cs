using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    public Transform player;

    [Header("Mouse Settings")]

    [Tooltip("Do nhay chuot goc. Gia tri cao → xoay nhanh hon")]
    public float mouseSensitivity = 100f;

    [Tooltip("He so nhan them cho sensitivity. Dung de scale linh hoat")]
    public float sensitivityMultiplier = 1f;

    [Header("Cursor Toggle")]
    [Tooltip("Phim de chuyen doi trang thai con tro")]
    public Key toggleCursorKey = Key.LeftAlt;

    private float xRotation = 0f;

    private bool canLook = true;
    private bool forceUnlock = false;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        if (keyboard == null || mouse == null) return;

        bool holdKey = keyboard[toggleCursorKey].isPressed;

        // ===== BUILD MODE ƯU TIÊN =====
        if (forceUnlock)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        // ===== ALT =====
        if (holdKey)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // ===== KHÔNG XOAY KHI ALT =====
        if (!canLook || holdKey) return;

        // ===== LOOK =====
        Vector2 mouseDelta = mouse.delta.ReadValue();

        float sensitivity = mouseSensitivity * sensitivityMultiplier;

        float mouseX = mouseDelta.x * sensitivity * Time.deltaTime;
        float mouseY = mouseDelta.y * sensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        player.Rotate(Vector3.up * mouseX);
    }

    public void SetLook(bool value)
    {
        canLook = value;
    }

    public void ForceCursorUnlock(bool value)
    {
        forceUnlock = value;
    }
}