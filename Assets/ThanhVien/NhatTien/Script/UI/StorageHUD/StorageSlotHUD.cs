using UnityEngine;
using UnityEngine.EventSystems;

/*
 * StorageSlotHUD.cs
 * Folder: ThanhVien/NhatTien/Script/UI/StorageHUD/
 * Người làm: TIẾN
 *
 * CHỨC NĂNG:
 * Gắn lên BẤT KỲ object nào trong kho (kể cả child, không cần có Collider trực tiếp).
 * Dùng Raycast từ Camera để phát hiện click — tìm StorageSlotHUD trên cả cha lẫn con.
 *
 * SETUP:
 * 1. Gắn script này vào object có WoodStorage/RiceStorage/StoneStorage
 *    (hoặc cùng object có BoxCollider đều được).
 * 2. Gán StorageHUD (object có StorageHUDPanel) vào field hudPanel.
 * 3. Chọn đúng Storage Type.
 * => Không cần [RequireComponent(Collider)].
 */
public class StorageSlotHUD : MonoBehaviour
{
    public enum StorageType { Wood, Rice, Stone }

    [Header("Loại kho")]
    public StorageType storageType = StorageType.Wood;

    [Header("References")]
    [Tooltip("Panel HUD chung. Gán StorageHUD GameObject vào đây.")]
    public StorageHUDPanel hudPanel;

    [Tooltip("Tự tìm nếu bỏ trống.")]
    public WoodStorage  woodStorage;
    public RiceStorage  riceStorage;
    public StoneStorage stoneStorage;

    void Awake()
    {
        if (woodStorage  == null) woodStorage  = GetComponentInParent<WoodStorage>()  ?? GetComponentInChildren<WoodStorage>();
        if (riceStorage  == null) riceStorage  = GetComponentInParent<RiceStorage>()  ?? GetComponentInChildren<RiceStorage>();
        if (stoneStorage == null) stoneStorage = GetComponentInParent<StoneStorage>() ?? GetComponentInChildren<StoneStorage>();
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 500f)) return;

        // Kiểm tra xem hit object có phải kho này không (kể cả tìm trong cha/con)
        StorageSlotHUD clicked = hit.collider.GetComponent<StorageSlotHUD>()
                              ?? hit.collider.GetComponentInChildren<StorageSlotHUD>()
                              ?? hit.collider.GetComponentInParent<StorageSlotHUD>();

        if (clicked != this) return;

        // Đúng kho này → hiện HUD
        if (hudPanel == null)
        {
            Debug.LogWarning($"[StorageSlotHUD] '{name}': Chưa gán hudPanel!");
            return;
        }

        switch (storageType)
        {
            case StorageType.Wood:
                if (woodStorage  != null) hudPanel.ShowWood(woodStorage,   gameObject.name, transform.position);
                else Debug.LogWarning($"[StorageSlotHUD] '{name}': Không tìm thấy WoodStorage!");
                break;
            case StorageType.Rice:
                if (riceStorage  != null) hudPanel.ShowRice(riceStorage,   gameObject.name, transform.position);
                else Debug.LogWarning($"[StorageSlotHUD] '{name}': Không tìm thấy RiceStorage!");
                break;
            case StorageType.Stone:
                if (stoneStorage != null) hudPanel.ShowStone(stoneStorage, gameObject.name, transform.position);
                else Debug.LogWarning($"[StorageSlotHUD] '{name}': Không tìm thấy StoneStorage!");
                break;
        }
    }
}
