using UnityEngine;
using UnityEngine.EventSystems;

/*
 * UnlockWorldUI.cs
 * Gắn trực tiếp vào Object Vùng Lock mới tạo.
 * CƠ CHẾ: Chạy độc lập hoàn toàn, tự quét Raycast 3D bằng Layer riêng để tránh xung đột với hệ thống chọn nhà.
 */
public class UnlockWorldUI : MonoBehaviour
{
    [Header("Cấu hình Quét Chuột Độc Lập")]
    [Tooltip("Tạo một Layer mới tên là LockZone và gán vào đây")]
    public LayerMask lockZoneLayerMask;
    public float maxRayDistance = 150f;

    private UnlockableEntity _parentEntity;

    private void Start()
    {
        _parentEntity = GetComponent<UnlockableEntity>();
    }

    void Update()
    {
        // Khi người chơi click chuột trái
        if (Input.GetMouseButtonDown(0))
        {
            TrySelectLockZone();
        }
    }

    private void TrySelectLockZone()
    {
        // 1. Chặn click xuyên qua nếu chuột đang đè lên UI phẳng (HUD tài nguyên, nút bấm khác)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // 2. Bắn tia Raycast từ Camera chính đến tọa độ chuột
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        
        // 3. Quét kiểm tra xem có va chạm trúng chính xác Layer của Object Lock này không
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, lockZoneLayerMask))
        {
            // Kiểm tra xem Collider bị bắn trúng có phải là của chính Object này hoặc con của nó không
            if (hit.collider.gameObject == this.gameObject || hit.transform.IsChildOf(this.transform))
            {
                Debug.Log($"[UnlockSystem] 🎯 Raycast độc lập nhận diện CLICK THÀNH CÔNG: {gameObject.name}");

                if (_parentEntity != null && _parentEntity.IsLocked)
                {
                    if (UnlockDetailPanel.Instance != null)
                    {
                        UnlockDetailPanel.Instance.ShowPanel(_parentEntity);
                    }
                    else
                    {
                        Debug.LogError("[UnlockSystem] ❌ Không tìm thấy Instance của UnlockDetailPanel trong Scene!");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Xóa bỏ hoàn toàn Object này ra khỏi bản đồ khi mở khóa thành công
    /// </summary>
    public void DestroyLockZone()
    {
        Debug.Log($"[UnlockSystem] 🔥 Hủy bỏ hoàn toàn Object Lock độc lập: {gameObject.name}");
        Destroy(gameObject);
    }
}