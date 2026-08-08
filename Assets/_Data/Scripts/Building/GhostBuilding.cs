using UnityEngine;

public class GhostBuilding : MonoBehaviour
{
    [Header("Mảng Model Ghost theo Cấp Độ")]
    public GameObject[] levelModels;

    [Header("Loại công trình đang đặt")]
    public BuildingType buildingType;

    [Header("Kích thước Grid chiếm dụng")]
    [SerializeField] private int sizeX = 1;
    [SerializeField] private int sizeZ = 1;

    [Header("Materials")]
    public Material validMat;   // Xanh
    public Material invalidMat; // Đỏ

    [Header("Layer Settings")]
    public LayerMask groundLayer;   
    public LayerMask buildingLayer; 

    [Header("Settings")]
    public float checkYSize = 2f;

    private Renderer[] renderers;
    private Collider ghostCollider;
    public bool isValid = false; // Đổi private -> public để BuildingSystem kiểm tra được
    private float currentYRot = 0f;
    private const float ROT_STEP = 90f;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        ghostCollider = GetComponentInChildren<Collider>();
    }

    private void Update()
    {
        FollowMouse();
        HandleRotateInput();
        HandleConfirmInput();
        HandleCancelInput();
    }

    private void FollowMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        bool hasHit = Physics.Raycast(ray, out hit, 1000f, groundLayer);
        if (!hasHit)
        {
            hasHit = Physics.Raycast(ray, out hit, 1000f);
        }

        if (!hasHit)
        {
            isValid = false;
            ApplyMaterial(invalidMat);
            return;
        }

        // Snap vị trí dựa theo kích thước SizeX & SizeZ của công trình
        transform.position = SnapToGrid(hit.point);
        CheckValidity();
    }

    private Vector3 SnapToGrid(Vector3 rawWorldPos)
    {
        return rawWorldPos;
    }

    private void CheckValidity()
    {
        isValid = true;
        ApplyMaterial(validMat);
    }

    // private bool IsOverlapping()
    // {
    //     if (ghostCollider == null) return false;

    //     Vector3 center = ghostCollider.bounds.center;
    //     Vector3 halfSize = ghostCollider.bounds.extents;
    //     halfSize.y = checkYSize / 2f;

    //     Collider[] hits = Physics.OverlapBox(center, halfSize, transform.rotation, buildingLayer);

    //     foreach (var hit in hits)
    //     {
    //         if (hit.transform.root != transform.root) return true;
    //     }

    //     return false;
    // }

    private void HandleRotateInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            currentYRot = (currentYRot + ROT_STEP) % 360f;
            transform.rotation = Quaternion.Euler(0f, currentYRot, 0f);
        }
    }

    private void HandleConfirmInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        if (!isValid)
        {
            Debug.LogWarning($"[GhostBuilding] Vị trí không hợp lệ để xây {buildingType}!");
            return;
        }

        ConfirmPlace();
    }

    private void HandleCancelInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            CancelPlace();
        }
    }

    private void ConfirmPlace()
    {
        ConstructionManager.Ins.PlaceBuilding(buildingType, transform.position, Quaternion.Euler(0f, currentYRot, 0f));
        BuildingSystem.Ins.OnPlacingCompleted(false);
        Destroy(gameObject);
    }

    private void CancelPlace()
    {
        BuildingSystem.Ins.OnPlacingCompleted(true);
        Destroy(gameObject);
    }

    private void ApplyMaterial(Material mat)
    {
        if (mat == null) return;
        foreach (var r in renderers) r.material = mat;
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);

    public void SetGhostLevel(int level)
    {
        if (levelModels == null || levelModels.Length == 0) return;
        for (int i = 0; i < levelModels.Length; i++)
        {
            if (levelModels[i] != null) levelModels[i].SetActive(i == level);
        }
    }

    // 👇 THÊM ĐOẠN NÀY VÀO NGAY DƯỚI:
    public void InstantSnapToMouse()
    {
        FollowMouse();
    }
}