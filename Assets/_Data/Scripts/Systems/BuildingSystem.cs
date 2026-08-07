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
        }
    }

    public void StartPlacing(BuildingType type)
    {
        if (type == BuildingType.None) return;

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

        LandGridManager.Ins?.SetGridVisualActive(true);

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

        LandGridManager.Ins?.SetGridVisualActive(false);

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
        LandGridManager.Ins?.SetGridVisualActive(false);

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

        // 🔥 XÓA ĐÁNH DẤU Ô CŨ TRÊN GRID (Để vị trí cũ tạm thời trống)
        LandGridManager.Ins?.UnmarkAreaAsOccupied(_movingBuilding.transform.position);

        LandGridManager.Ins?.SetGridVisualActive(true);

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

                // 🔥 ĐÁNH DẤU VỊ TRÍ MỚI ĐÃ BỊ CHIẾM
                LandGridManager.Ins?.MarkAreaAsOccupied(newPosition);

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
        LandGridManager.Ins?.SetGridVisualActive(false);

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

            // 🔥 NẾU HỦY DI CHUYỂN, ĐÁNH DẤU LẠI VỊ TRÍ CŨ
            LandGridManager.Ins?.MarkAreaAsOccupied(_movingBuilding.transform.position);
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