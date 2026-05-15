using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("References")]
    public Transform cameraTransform;

    [Header("Distance")]
    public float distance = 4f;
    public float minDistance = 1f;

    [Header("Height")]
    public float height = 1.5f;

    [Header("Mouse")]
    public float mouseSensitivity = 120f;

    [Header("Smooth")]
    public float followSmooth = 10f;
    public float rotationSmooth = 10f;
    public float collisionSmooth = 20f;

    [Header("Pitch")]
    public float minPitch = -30f;
    public float maxPitch = 70f;

    [Header("Collision")]
    public LayerMask collisionLayers;
    public float sphereRadius = 0.2f;

    [Header("Cursor")]
    public Key toggleCursorKey = Key.LeftAlt;

    private float yaw;
    private float pitch;

    private float currentDistance;

    private bool canLook = true;
    private bool forceUnlock = false;

    void Start()
    {
        currentDistance = distance;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
    }

    void LateUpdate()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        if (keyboard == null || mouse == null) return;

        bool holdKey = keyboard[toggleCursorKey].isPressed;

        // ===== CURSOR =====

        if (forceUnlock || holdKey)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (!canLook) return;

        // ===== INPUT =====

        Vector2 mouseDelta = mouse.delta.ReadValue();

        yaw += mouseDelta.x * mouseSensitivity * Time.deltaTime;
        pitch -= mouseDelta.y * mouseSensitivity * Time.deltaTime;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // ===== ROTATION =====

        Quaternion targetRotation =
            Quaternion.Euler(pitch, yaw, 0f);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSmooth * Time.deltaTime
        );

        // ===== FOLLOW =====

        Vector3 targetPosition =
            target.position +
            Vector3.up * height;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSmooth * Time.deltaTime
        );

        // ===== COLLISION =====

        HandleCollision();
    }

    void HandleCollision()
    {
        Vector3 desiredCameraPosition =
            transform.position -
            transform.forward * distance;

        Vector3 direction =
            desiredCameraPosition - transform.position;

        float targetDistance = distance;

        if (Physics.SphereCast(
            transform.position,
            sphereRadius,
            direction.normalized,
            out RaycastHit hit,
            distance,
            collisionLayers))
        {
            targetDistance = hit.distance - 0.2f;

            targetDistance =
                Mathf.Clamp(
                    targetDistance,
                    minDistance,
                    distance
                );
        }

        currentDistance = Mathf.Lerp(
            currentDistance,
            targetDistance,
            collisionSmooth * Time.deltaTime
        );

        cameraTransform.position =
            transform.position -
            transform.forward * currentDistance;

        cameraTransform.rotation = transform.rotation;
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