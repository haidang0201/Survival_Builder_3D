using UnityEngine;

public class BuildingPlacement : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject ghostPrefab;
    public GameObject constructionPrefab;
    
    private GameObject currentGhost;
    private Camera mainCamera;
    private LayerMask terrainLayer;

    void Start()
    {
        InitializeSetup();
    }

    void Update()
    {
        if (currentGhost != null)
        {
            HandlePlacement();
        }
    }

    // --- CÁC HÀM KHỞI TẠO VÀ ĐIỀU KHIỂN CHÍNH ---

    private void InitializeSetup()
    {
        mainCamera = Camera.main;
        terrainLayer = LayerMask.GetMask("Terrain");
    }

    public void StartPlacement() // Gọi hàm này từ UI Button để bắt đầu xây
    {
        if (currentGhost == null)
        {
            currentGhost = Instantiate(ghostPrefab);
        }
    }

    private void HandlePlacement()
    {
        RaycastHit hit;
        if (PerformRaycast(out hit))
        {
            UpdateGhostPosition(hit.point);
            CheckInputAndPlace();
        }
    }

    // --- CÁC HÀM XỬ LÝ LOGIC CHI TIẾT ---

    private bool PerformRaycast(out RaycastHit hit)
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(ray, out hit, 100f, terrainLayer);
    }

    private void UpdateGhostPosition(Vector3 hitPoint)
    {
        Vector3 snappedPos = GetSnappedPosition(hitPoint);
        currentGhost.transform.position = snappedPos;
    }

    private Vector3 GetSnappedPosition(Vector3 rawPos)
    {
        // Làm tròn tọa độ để dính vào ô lưới (Grid Snapping)
        return new Vector3(Mathf.Round(rawPos.x), rawPos.y, Mathf.Round(rawPos.z));
    }

    private void CheckInputAndPlace()
    {
        if (Input.GetMouseButtonDown(0))
        {
            AttemptToBuild();
        }
    }

    private void AttemptToBuild()
    {
        GhostValidator validator = currentGhost.GetComponent<GhostValidator>();
        
        if (validator != null && validator.IsValidPosition())
        {
            SpawnConstructionSite(currentGhost.transform.position);
            ClearGhost();
        }
        else
        {
            Debug.Log("Vị trí bị vướng, không thể xây!");
        }
    }

    private void SpawnConstructionSite(Vector3 position)
    {
        Instantiate(constructionPrefab, position, Quaternion.identity);
    }

    private void ClearGhost()
    {
        Destroy(currentGhost);
    }
}