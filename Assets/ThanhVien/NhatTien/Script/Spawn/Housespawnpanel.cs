using UnityEngine;
using UnityEngine.EventSystems;

/*
 * HouseSpawnPanel.cs
 * Folder: Scripts/Spawning/
 * Dự án: KHẨN HOANG (PENTA DEV)
 *
 * CHỨC NĂNG:
 * Gắn lên GameObject House (cùng object có script House.cs, cần có Collider để bấm được).
 * Khi bấm chuột vào House -> hiện panel UI có 3 nút Tree/Rice/Stone.
 * Bấm nút nào -> gọi WorkerSpawner.Instance.SpawnWorker(loại tương ứng, EntrancePosition của House này).
 *
 * KHÔNG sửa House.cs. Script này đứng độc lập bên cạnh, chỉ đọc EntrancePosition qua
 * property public đã có sẵn.
 *
 * SETUP TRONG UNITY:
 * 1. Gắn script này vào cùng GameObject với House.cs (object đó cần có Collider,
 *    ví dụ BoxCollider bao quanh nhà, để OnMouseDown() nhận được click).
 * 2. Tạo 1 Canvas trong Scene, bên trong có:
 *    - 1 Panel (đặt tên "WorkerSpawnPanel") chứa 3 Button: "Tree", "Rice", "Stone".
 *    - Panel này để SetActive(false) sẵn lúc đầu.
 * 3. Kéo Panel đó vào field spawnPanel bên dưới.
 * 4. Kéo 3 Button tương ứng vào 3 field treeButton/riceButton/stoneButton (script tự
 *    gắn onClick bằng code, không cần bạn tự nối onClick trong Inspector).
 * 5. Đảm bảo Scene đã có WorkerSpawner (xem WorkerSpawner.cs) với 3 prefab được gán.
 */
[RequireComponent(typeof(Collider))]
public class HouseSpawnPanel : MonoBehaviour
{
    [Header("References")]
    [Tooltip("House gắn cùng object này. Nếu để trống sẽ tự GetComponent lúc Awake.")]
    public House house;

    [Header("UI")]
    [Tooltip("Panel chứa các nút spawn worker. Sẽ được Show/Hide khi bấm vào House.")]
    public GameObject spawnPanel;

    public UnityEngine.UI.Button treeButton;
    public UnityEngine.UI.Button riceButton;
    public UnityEngine.UI.Button stoneButton;

    [Header("Options")]
    [Tooltip("Nếu bật, bấm ra ngoài panel (chuột trái ở nơi khác) sẽ tự đóng panel.")]
    public bool closeOnClickOutside = true;

    void Awake()
    {
        if (house == null) house = GetComponent<House>();

        if (treeButton != null) treeButton.onClick.AddListener(() => SpawnAndClose(WorkerSpawner.WorkerType.Tree));
        if (riceButton != null) riceButton.onClick.AddListener(() => SpawnAndClose(WorkerSpawner.WorkerType.Rice));
        if (stoneButton != null) stoneButton.onClick.AddListener(() => SpawnAndClose(WorkerSpawner.WorkerType.Stone));

        if (spawnPanel != null) spawnPanel.SetActive(false);
    }

    void Update()
    {
        if (!closeOnClickOutside) return;
        if (spawnPanel == null || !spawnPanel.activeSelf) return;

        if (Input.GetMouseButtonDown(0) && !IsPointerOverPanel())
        {
            // Nếu click ngay lên House thì OnMouseDown() bên dưới sẽ tự xử lý, không cần đóng ở đây.
            bool clickedOnThisHouse = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 500f)
                                       && hit.collider != null
                                       && hit.collider.gameObject == gameObject;
            if (!clickedOnThisHouse)
                HidePanel();
        }
    }

    bool IsPointerOverPanel()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    void OnMouseDown()
    {
        // Tránh trigger khi đang bấm vào UI (nút bấm) nằm đè lên world object
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        ShowPanel();
    }

    void ShowPanel()
    {
        if (spawnPanel != null) spawnPanel.SetActive(true);
    }

    void HidePanel()
    {
        if (spawnPanel != null) spawnPanel.SetActive(false);
    }

    void SpawnAndClose(WorkerSpawner.WorkerType type)
    {
        if (WorkerSpawner.Instance == null)
        {
            Debug.LogError("[HouseSpawnPanel] Không tìm thấy WorkerSpawner.Instance trong Scene.");
            return;
        }
        if (house == null)
        {
            Debug.LogError("[HouseSpawnPanel] Thiếu reference tới House.");
            return;
        }

        WorkerSpawner.Instance.SpawnWorker(type, house.EntrancePosition);
        HidePanel();
    }
}