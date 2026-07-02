using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonWobble : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Cấu hình hiệu ứng lắc")]
    [SerializeField] private float wobbleSpeed = 15f;    // Tốc độ lắc
    [SerializeField] private float wobbleAngle = 5f;    // Biên độ góc lắc (độ)

    private bool isHovered = false;
    private Quaternion originalRotation;
    private float timeElapsed;

    void Start()
    {
        // Lưu lại góc xoay ban đầu của nút
        originalRotation = transform.localRotation;
    }

    void Update()
    {
        if (isHovered)
        {
            timeElapsed += Time.deltaTime * wobbleSpeed;
            // Sử dụng hàm Sin để tạo chuyển động qua lại mượt mà
            float zRotation = Mathf.Sin(timeElapsed) * wobbleAngle;
            transform.localRotation = originalRotation * Quaternion.Euler(0, 0, zRotation);
        }
        else
        {
            // Khi không hover, trả nút về trạng thái thẳng ban đầu một cách mượt mà
            transform.localRotation = Quaternion.Lerp(transform.localRotation, originalRotation, Time.deltaTime * 10f);
            timeElapsed = 0f;
        }
    }

    // Hàm thực thi khi con trỏ chuột đi vào vùng của Nút
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    // Hàm thực thi khi con trỏ chuột rời khỏi vùng của Nút
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }
}