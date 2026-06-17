using UnityEngine;

/*
 * UIBillboard.cs
 * Folder: Scripts/UI/
 * Dự án: KHẨN HOANG (PENTA DEV)
 * NHIỆM VỤ: Khiến thanh tiến độ (Canvas World Space) luôn luôn xoay mặt hướng về phía Camera.
 */
public class UIBillboard : MonoBehaviour
{
    private Transform _mainCameraTransform;

    void Start()
    {
        // Lấy dữ liệu Transform của Main Camera một lần ở Start để tối ưu hiệu năng
        if (Camera.main != null)
        {
            _mainCameraTransform = Camera.main.transform;
        }
    }

    // Dùng LateUpdate để đảm bảo thanh tiến độ xoay SAU KHI Camera đã di chuyển xong ở Update
    void LateUpdate()
    {
        if (_mainCameraTransform == null) return;

        // Ép Object luôn có hướng nhìn trùng với hướng nhìn của Camera
        transform.LookAt(transform.position + _mainCameraTransform.rotation * Vector3.forward,
            _mainCameraTransform.rotation * Vector3.up);
    }
}