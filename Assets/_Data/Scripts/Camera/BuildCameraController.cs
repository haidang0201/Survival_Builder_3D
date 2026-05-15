using UnityEngine;
using UnityEngine.InputSystem;

public class BuildCameraController : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 15f;

    [Header("Zoom")]
    public float zoomSpeed = 200f;
    public float minZoom = 10f;
    public float maxZoom = 60f;

    [Header("Rotation")]
    public float rotateSpeed = 100f;

    [Header("Tilt")]
    public float tiltSpeed = 50f;
    public float minTilt = 30f;
    public float maxTilt = 80f;

    private bool isActive = false;

    private float currentYaw = 0f;
    private float currentTilt = 60f;

    public void SetActive(bool value)
    {
        isActive = value;
        gameObject.SetActive(value);
    }

    void Update()
    {
        if (!isActive) return;

        MoveCamera();
        Zoom();
        Rotate();
        Tilt();
        ApplyRotation();
    }

    // ================= MOVE =================

    void MoveCamera()
    {
        Vector3 dir = Vector3.zero;

        if (Keyboard.current.wKey.isPressed)
            dir += Vector3.forward;

        if (Keyboard.current.sKey.isPressed)
            dir += Vector3.back;

        if (Keyboard.current.aKey.isPressed)
            dir += Vector3.left;

        if (Keyboard.current.dKey.isPressed)
            dir += Vector3.right;

        transform.Translate(
            dir * moveSpeed * Time.deltaTime,
            Space.Self
        );
    }

    // ================= ZOOM =================

    void Zoom()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;

        Vector3 pos = transform.position;

        pos.y -= scroll * zoomSpeed * Time.deltaTime;

        pos.y = Mathf.Clamp(
            pos.y,
            minZoom,
            maxZoom
        );

        transform.position = pos;
    }

    // ================= ROTATE =================

    void Rotate()
    {
        if (Keyboard.current.qKey.isPressed)
        {
            currentYaw -= rotateSpeed * Time.deltaTime;
        }

        if (Keyboard.current.eKey.isPressed)
        {
            currentYaw += rotateSpeed * Time.deltaTime;
        }

        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();

            currentYaw +=
                delta.x *
                rotateSpeed *
                Time.deltaTime;
        }
    }

    // ================= TILT =================

    void Tilt()
    {
        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();

            currentTilt -=
                delta.y *
                tiltSpeed *
                Time.deltaTime;

            currentTilt = Mathf.Clamp(
                currentTilt,
                minTilt,
                maxTilt
            );
        }
    }

    // ================= APPLY =================

    void ApplyRotation()
    {
        transform.rotation =
            Quaternion.Euler(
                currentTilt,
                currentYaw,
                0f
            );
    }
}