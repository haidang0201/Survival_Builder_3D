using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * BuildingSystem.cs
 * Folder: Scripts/Building/
 * Dự án: KHẨN HOANG (PENTA DEV)
 */

public class BuildingSystem : Singleton<BuildingSystem>
{
    [Header("Ghost Prefabs – Dân sự")]
    public GameObject ghostHousePrefab;
    public GameObject ghostWoodCutterPrefab;
    public GameObject ghostStoneMinePrefab;
    public GameObject ghostKitchenPrefab;
    public GameObject ghostFoodStoragePrefab;
    public GameObject ghostStoneStoragePrefab;
    public GameObject ghostWarehousePrefab;

    [Header("Ghost Prefabs – Phòng thủ")]
    public GameObject ghostWatchTowerPrefab;
    public GameObject ghostArcherTowerPrefab;
    public GameObject ghostCannonPrefab;

    [Header("Ghost Prefabs – Quân sự (Nhà lính)")]
    public GameObject ghostBarracksMeleePrefab;
    public GameObject ghostBarracksArcherPrefab;
    public GameObject ghostBarracksSpearPrefab;

    [Header("Ghost Prefabs – Tài nguyên")]
    public GameObject ghostWoodPrefab;
    public GameObject ghostRicePrefab;
    public GameObject ghostStonePrefab;

    [Header("Demacia Rising – Khung chọn ô đất")]
    public GameObject slotHighlightPrefab;
    private GameObject slotHighlightInstance;
    private Vector3 selectedSlotPos;
    private bool hasSelectedSlot = false;

    public bool HasSelectedSlot => hasSelectedSlot;
    public Vector3 SelectedSlotPos => selectedSlotPos;

    private GhostBuilding currentGhost;
    private bool isPlacing = false;

    private UpgradeableBuilding _movingBuilding = null; 
    private bool _isMovingMode = false;

    public bool IsPlacing => isPlacing;
    public bool IsMovingMode => _isMovingMode;

    private void Update()
    {
        if (_isMovingMode)
        {
            HandlePlacementInput();
            return;
        }

        HandleSlotSelectionInput();
    }

    private void HandleSlotSelectionInput()
    {
        // Phím chuột phải hoặc ESC để bỏ chọn ô đất và đóng menu
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            if (hasSelectedSlot)
            {
                DeselectSlot();
                if (UIManager.Ins != null) UIManager.Ins.CloseBuildMenu();
            }
            return;
        }

        // Click chuột trái vào ô đất trống trên bản đồ
        if (Input.GetMouseButtonDown(0))
        {
            // Bỏ qua nếu bấm vào UI
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (currentGhost != null) return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                SelectSlot(hit.point);
                if (UIManager.Ins != null) UIManager.Ins.OpenSettlementPanel();
            }
        }
    }

    public void SelectSlot(Vector3 worldPos)
    {
        selectedSlotPos = worldPos;
        hasSelectedSlot = true;

        if (slotHighlightPrefab != null)
        {
            if (slotHighlightInstance == null)
            {
                slotHighlightInstance = Instantiate(slotHighlightPrefab);
            }
            slotHighlightInstance.transform.position = worldPos;
            slotHighlightInstance.SetActive(true);
        }
    }

    public void DeselectSlot()
    {
        hasSelectedSlot = false;
        if (slotHighlightInstance != null)
        {
            slotHighlightInstance.SetActive(false);
        }
    }

    public void StartPlacing(BuildingType type)
    {
        if (type == BuildingType.None) return;

        // 🔥 DEMACIA RISING STYLE: NẾU ĐÃ CHỌN Ô ĐẤT, XÂY TRỰC TIẾP TẠI Ô ĐẤT ĐÓ
        if (hasSelectedSlot)
        {
            ConstructionManager.Ins.PlaceBuilding(type, selectedSlotPos, Quaternion.identity);
            DeselectSlot();
            if (UIManager.Ins != null) UIManager.Ins.CloseBuildMenu();
            return;
        }

        // FALLBACK: NẾU CHƯA CHỌN Ô ĐẤT, KÍCH HOẠT CHẾ ĐỘ RÊ CHUỘT GHOST CŨ
        StartPlacingGhost(type);
    }

    public void StartPlacingGhost(BuildingType type)
    {
        if (_isMovingMode) CancelMoving();
        else CancelPlacing();

        GameObject prefab = GetGhostPrefab(type);
        if (prefab == null) return;

        GameObject obj = Instantiate(prefab);
        currentGhost = obj.GetComponent<GhostBuilding>();

        if (currentGhost == null)
        {
            Destroy(obj);
            return;
        }

        currentGhost.buildingType = type;
        currentGhost.InstantSnapToMouse();

        // 🔥 CẬP NHẬT TUTORIAL: Báo cho Tutorial Manager người chơi đã bắt đầu chế độ đặt nhà
        if (CampaignTutorialManager.Ins != null)
        {
            CampaignTutorialManager.Ins.OnStartPlacement();
        }

        if (UIManager.Ins != null)
        {
            UIManager.Ins.EnterPlacementMode();
        }
    }

    public void CancelPlacing()
    {
        if (currentGhost != null)
        {
            Destroy(currentGhost.gameObject);
            currentGhost = null;
        }

        // 🔥 CẬP NHẬT TUTORIAL: Báo cho Tutorial Manager khi hủy đặt nhà
        if (CampaignTutorialManager.Ins != null)
        {
            CampaignTutorialManager.Ins.OnCancelPlacement();
        }

        if (UIManager.Ins != null)
        {
            UIManager.Ins.ExitPlacementMode(true);
        }
    }

    public void OnPlacingCompleted(bool shouldReopenMenu)
    {
        currentGhost = null;

        if (UIManager.Ins != null)
        {
            UIManager.Ins.ExitPlacementMode(shouldReopenMenu);
        }
    }

    public void StartMoving(UpgradeableBuilding building)
    {
        if (building == null) return;

        if (isPlacing) CancelPlacing();
        if (_isMovingMode) CancelMoving();

        _movingBuilding = building;
        _isMovingMode = true;

        _movingBuilding.PauseBuildingProcess();
        _movingBuilding.gameObject.SetActive(false);

        BuildingType currentType = building.buildingType; 

        GameObject prefab = GetGhostPrefab(currentType);
        if (prefab != null)
        {
            GameObject obj = Instantiate(prefab, building.transform.position, building.transform.rotation);
            currentGhost = obj.GetComponent<GhostBuilding>();
            if (currentGhost != null)
            {
                currentGhost.buildingType = currentType;
                currentGhost.SetGhostLevel(building.CurrentLevel);
                currentGhost.InstantSnapToMouse();
            }
        }

        if (UIManager.Ins != null)
        {
            UIManager.Ins.EnterPlacementMode();
        }
    }

    private void HandlePlacementInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (currentGhost == null || _movingBuilding == null) return;

            bool isValidPosition = currentGhost != null && currentGhost.isValid; 
            if (isValidPosition)
            {
                Vector3 newPosition = currentGhost.transform.position;
                Quaternion newRotation = currentGhost.transform.rotation;

                _movingBuilding.transform.position = newPosition;
                _movingBuilding.transform.rotation = newRotation;
                _movingBuilding.gameObject.SetActive(true);

                _movingBuilding.ResumeBuildingProcess();

                if (currentGhost != null)
                {
                    Destroy(currentGhost.gameObject);
                    currentGhost = null;
                }

                SaveBuildingsToSlot(1);
                EndMovingMode();
            }
            else
            {
                if (UIManager.Ins != null) 
                    UIManager.Ins.ShowWarning("Vị trí mới bị cản trở bởi vật thể khác, không thể đặt nhà!");
            }
        }

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelMoving();
        }
    }

    private void EndMovingMode()
    {
        _isMovingMode = false;
        _movingBuilding = null;
        currentGhost = null;

        if (UIManager.Ins != null)
        {
            UIManager.Ins.ExitPlacementMode(false);
        }
    }

    private void CancelMoving()
    {
        if (currentGhost != null)
        {
            Destroy(currentGhost.gameObject);
            currentGhost = null;
        }

        if (_movingBuilding != null)
        {
            _movingBuilding.gameObject.SetActive(true);
            _movingBuilding.ResumeBuildingProcess();
        }

        EndMovingMode();
    }

    public void SaveBuildings() => SaveBuildingsToSlot(1);
    public void LoadBuildings() => LoadBuildingsFromSlot(1);

    public void SaveBuildingsToSlot(int slotIndex)
    {
        if (BuildingManager.Ins == null) return;
        var states = BuildingManager.Ins.GetAllStates();

        if (states.Count == 0) return;

        var saveData = new JsonDataManager.GameSaveData
        {
            sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            savedAtUnix = System.DateTimeOffset.Now.ToUnixTimeSeconds(),
            buildings = states,
            resources = new System.Collections.Generic.List<JsonDataManager.ResourceData>()
        };

        JsonDataManager.Ins.SaveGame(slotIndex, saveData);
    }

    public void LoadBuildingsFromSlot(int slotIndex)
    {
        if (JsonDataManager.Ins == null || BuildingManager.Ins == null) return;
        var saveData = JsonDataManager.Ins.LoadGame(slotIndex);

        if (saveData == null || saveData.buildings == null || saveData.buildings.Count == 0) return;

        BuildingManager.Ins.LoadStates(saveData.buildings);
    }

    private GameObject GetGhostPrefab(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.House: return ghostHousePrefab;
            case BuildingType.WoodCutter: return ghostWoodCutterPrefab;
            case BuildingType.StoneMine: return ghostStoneMinePrefab;
            case BuildingType.Kitchen: return ghostKitchenPrefab;
            case BuildingType.FoodStorage: return ghostFoodStoragePrefab;
            case BuildingType.StoneStorage: return ghostStoneStoragePrefab;
            case BuildingType.Warehouse: return ghostWarehousePrefab;
            case BuildingType.WatchTower: return ghostWatchTowerPrefab;
            case BuildingType.ArcherTower: return ghostArcherTowerPrefab;
            case BuildingType.Cannon: return ghostCannonPrefab;
            case BuildingType.BarracksMelee: return ghostBarracksMeleePrefab;
            case BuildingType.BarracksArcher: return ghostBarracksArcherPrefab;
            case BuildingType.BarracksSpear: return ghostBarracksSpearPrefab;
            case BuildingType.Wood: return ghostWoodPrefab;
            case BuildingType.Rice: return ghostRicePrefab;
            case BuildingType.Stone: return ghostStonePrefab;
            default: return null;
        }
    }
}