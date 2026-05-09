using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;

    [Header("Movement")]
    public float speed = 5f;
    public float runMultiplier = 2f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;

    [Header("Animation")]
    public int upperBodyLayerIndex = 1;

    private CharacterController controller;
    private Animator animator;

    private Vector2 moveInput;
    private float yVelocity;
    private bool canMove = true;

    private bool isChopping = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }

    public void SetMove(bool value)
    {
        canMove = value;
    }

    void Update()
    {
        if (!canMove) return;

        HandleInput();
        ApplyGravity();
        Move();
        UpdateAnimation();
        HandleChop();
    }

    // ================= INPUT =================

    void HandleInput()
    {
        moveInput = Vector2.zero;

        if (Keyboard.current.wKey.isPressed) moveInput.y += 1;
        if (Keyboard.current.sKey.isPressed) moveInput.y -= 1;
        if (Keyboard.current.aKey.isPressed) moveInput.x -= 1;
        if (Keyboard.current.dKey.isPressed) moveInput.x += 1;

        moveInput = Vector2.ClampMagnitude(moveInput, 1f);
    }

    // ================= GRAVITY =================

    void ApplyGravity()
    {
        if (controller.isGrounded && yVelocity < 0)
            yVelocity = -2f;

        yVelocity += gravity * Time.deltaTime;
    }

    // ================= MOVEMENT =================

    void Move()
    {
        bool isRunning = Keyboard.current.leftShiftKey.isPressed;

        float currentSpeed = isRunning
            ? speed * runMultiplier
            : speed;

        // huong camera
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        // huong movement
        Vector3 moveDirection =
            forward * moveInput.y +
            right * moveInput.x;

        // xoay player
        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(moveDirection);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        // gravity
        moveDirection.y = yVelocity;

        controller.Move(
            moveDirection *
            currentSpeed *
            Time.deltaTime
        );
    }

    // ================= ANIMATION =================

    void UpdateAnimation()
    {
        bool isRunning = Keyboard.current.leftShiftKey.isPressed;

        float speedParam = moveInput.magnitude;

        if (isRunning)
            speedParam *= 2f;

        speedParam = Mathf.Clamp01(speedParam);

        animator.SetFloat(
            "Speed",
            speedParam,
            0.1f,
            Time.deltaTime
        );
    }

    // ================= CHOP =================

    void HandleChop()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        isChopping = mouse.leftButton.isPressed;

        animator.SetLayerWeight(
            upperBodyLayerIndex,
            isChopping ? 1f : 0f
        );
    }
}