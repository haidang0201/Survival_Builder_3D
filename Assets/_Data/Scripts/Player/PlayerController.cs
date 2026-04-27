using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Cấu hình di chuyển")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -15f;
    [SerializeField] private float turnSmoothTime = 0.1f;

    [Header("Tham chiếu")]
    public Animator animator; 
    public Transform cameraTransform; 

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private float turnSmoothVelocity;

    // Các biến nhận dữ liệu từ người quản lý Input
    private Vector2 currentMovementInput;
    private bool jumpRequested;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null) cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        HandleMovement();
    }

    // ==========================================
    // CÁC HÀM NÀY ĐỂ THÀNH VIÊN KIA GỌI VÀO
    // ==========================================

    // Thành viên kia sẽ truyền Vector2(x, y) từ Input System vào đây
    public void SetMovementInput(Vector2 input)
    {
        currentMovementInput = input;
    }

    // Thành viên kia sẽ gọi hàm này khi người chơi bấm nút Nhảy
    public void TriggerJump()
    {
        jumpRequested = true;
    }

    // ==========================================

    void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Lấy Input từ biến thay vì gọi Input.GetAxis trực tiếp
        Vector3 inputDir = new Vector3(currentMovementInput.x, 0f, currentMovementInput.y).normalized;

        if (inputDir.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * moveSpeed * Time.deltaTime);
        }

        // Xử lý nhảy dựa trên request
        if (jumpRequested && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpRequested = false; // Reset sau khi nhảy
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        if (animator != null)
        {
            animator.SetFloat("Speed", inputDir.magnitude, 0.1f, Time.deltaTime); 
        }
    }
}