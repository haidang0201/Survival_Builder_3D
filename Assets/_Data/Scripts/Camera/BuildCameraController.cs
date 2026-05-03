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

    [Header("RTS Control")]
    public LayerMask groundLayer;
    public LayerMask playerLayer;
    public float moveToSpeed = 5f;

    private bool isActive = false;

    float currentYaw = 0f;
    float currentTilt = 60f;

    private Transform selectedPlayer;
    private Vector3 targetPosition;
    private bool isMovingPlayer = false;

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

        HandleMouseInput();
        MoveSelectedPlayer();
    }

    // ================= CAMERA =================

    void MoveCamera()
    {
        Vector3 dir = Vector3.zero;

        if (Keyboard.current.wKey.isPressed) dir += Vector3.forward;
        if (Keyboard.current.sKey.isPressed) dir += Vector3.back;
        if (Keyboard.current.aKey.isPressed) dir += Vector3.left;
        if (Keyboard.current.dKey.isPressed) dir += Vector3.right;

        transform.Translate(dir * moveSpeed * Time.deltaTime, Space.Self);
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
        if (Keyboard.current.qKey.isPressed)
            currentYaw -= rotateSpeed * Time.deltaTime;

        if (Keyboard.current.eKey.isPressed)
            currentYaw += rotateSpeed * Time.deltaTime;

        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            currentYaw += delta.x * rotateSpeed * Time.deltaTime;
        }
    }

    void Tilt()
    {
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

    // ================= RTS CONTROL =================

    void HandleMouseInput()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        // CHUỘT TRÁI → CHỌN PLAYER
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, playerLayer))
            {
                selectedPlayer = hit.transform;
                Debug.Log("Selected: " + selectedPlayer.name);
            }
        }

        // CHUỘT PHẢI → DI CHUYỂN
        if (Mouse.current.rightButton.wasPressedThisFrame && selectedPlayer != null)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
            {
                targetPosition = hit.point;
                isMovingPlayer = true;
            }
        }
    }

    void MoveSelectedPlayer()
    {
        if (!isMovingPlayer || selectedPlayer == null) return;

        Vector3 dir = (targetPosition - selectedPlayer.position);
        dir.y = 0;

        if (dir.magnitude < 0.1f)
        {
            isMovingPlayer = false;
            return;
        }

        selectedPlayer.position += dir.normalized * moveToSpeed * Time.deltaTime;
    }
}