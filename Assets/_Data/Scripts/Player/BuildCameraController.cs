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

    float currentYaw = 0f;
    float currentTilt = 60f;

    public void SetActive(bool value)
    {
        isActive = value;
        gameObject.SetActive(value);
    }

    void Update()
    {
        if (!isActive) return;

        Move();
        Zoom();
        Rotate();
        Tilt();

        ApplyRotation();
    }

    void Move()
    {
        Vector3 direction = Vector3.zero;

        if (Keyboard.current.wKey.isPressed) direction += Vector3.forward;
        if (Keyboard.current.sKey.isPressed) direction += Vector3.back;
        if (Keyboard.current.aKey.isPressed) direction += Vector3.left;
        if (Keyboard.current.dKey.isPressed) direction += Vector3.right;

        transform.Translate(direction * moveSpeed * Time.deltaTime, Space.Self);
    }

    void Zoom()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;

        Vector3 pos = transform.position;
        pos.y -= scroll * zoomSpeed * Time.deltaTime;
        pos.y = Mathf.Clamp(pos.y, minZoom, maxZoom);

        transform.position = pos;
    }

    void Rotate()
    {
        // Q / E xoay
        if (Keyboard.current.qKey.isPressed)
            currentYaw -= rotateSpeed * Time.deltaTime;

        if (Keyboard.current.eKey.isPressed)
            currentYaw += rotateSpeed * Time.deltaTime;

        // giữ chuột phải để xoay tự do
        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            currentYaw += delta.x * rotateSpeed * Time.deltaTime;
        }
    }

    void Tilt()
    {
        // dùng chuột phải kéo lên xuống để tilt
        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            currentTilt -= delta.y * tiltSpeed * Time.deltaTime;
            currentTilt = Mathf.Clamp(currentTilt, minTilt, maxTilt);
        }
    }

    void ApplyRotation()
    {
        transform.rotation = Quaternion.Euler(currentTilt, currentYaw, 0f);
    }
}