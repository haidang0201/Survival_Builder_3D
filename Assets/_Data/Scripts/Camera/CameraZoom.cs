using UnityEngine;
using Unity.Cinemachine; 
using Game.Player; // Dùng namespace của bạn kia

public class CameraZoom : MonoBehaviour
{
    public CinemachineCamera vcam;
    public PlayerInputHandler inputHandler; // Kéo Player vào đây
    
    public float zoomSpeed = 5f;
    public float minSize = 4f;
    public float maxSize = 12f;

    void Update()
    {
        if (vcam == null || inputHandler == null) return;

        // Lấy giá trị Scroll đã được bạn kia xử lý
        float scrollInput = inputHandler.Scroll;

        if (scrollInput != 0)
        {
            // Đảm bảo Lens Mode là Orthographic trong Inspector
            float currentSize = vcam.Lens.OrthographicSize;
            
            // Tính toán size mới (chia 120f để chuẩn hóa tốc độ cuộn)
            currentSize -= (scrollInput / 1f) * zoomSpeed * Time.deltaTime;
            
            vcam.Lens.OrthographicSize = Mathf.Clamp(currentSize, minSize, maxSize);
        }
    }
}