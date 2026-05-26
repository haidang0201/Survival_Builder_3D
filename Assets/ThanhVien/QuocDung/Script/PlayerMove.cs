using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMove : MonoBehaviour
{
    public float speed = 5f;
    public float mouseSensitivity = 2f;
    public Transform playerCamera;

    private CharacterController controller;
    private float pitch = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Look();
        Move();
    }

    void Look()
    {
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;

        pitch -= my;
        pitch = Mathf.Clamp(pitch, -89f, 89f);
        if (playerCamera != null)
            playerCamera.localEulerAngles = new Vector3(pitch, 0f, 0f);

        transform.Rotate(Vector3.up * mx);
    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 dir = transform.right * x + transform.forward * z;
        controller.Move(dir * speed * Time.deltaTime);
    }
}
