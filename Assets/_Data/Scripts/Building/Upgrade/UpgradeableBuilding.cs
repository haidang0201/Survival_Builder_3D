using UnityEngine;
using System.Collections;

public class UpgradeableBuilding : MonoBehaviour
{
    [System.Serializable]
    public struct UpgradeCost
    {
        public int woodCost;
        public int stoneCost;
        public int foodCost;
        public float upgradeDuration; // Thời gian nâng cấp tính bằng giây
    }

    [Header("Penta Dev - Khởi Tạo Xây Dựng Ban Đầu")]
    [Tooltip("TÍCH VÀO: Công trình chưa xây. Khi vừa chạy game sẽ ép chạy thời gian, VFX, SFX như nâng cấp.\nTẮT TÍCH: Công trình đã xây xong từ trước, vào game sẽ không lặp lại.")]
    [SerializeField] private bool isInitialBuildNeeded = true;

    [Tooltip("Thời gian để hoàn thành việc xây dựng công trình này lần đầu tiên (tính bằng giây)")]
    [SerializeField] private float initialBuildDuration = 5f;

    // Cổng Property để UI đọc trạng thái xem nhà có phải đang trong luồng xây mới hay không
    public bool IsInitialBuildNeeded => isInitialBuildNeeded;

    [Header("Loại công trình")]
    public BuildingType buildingType;

    [Header("Tên công trình")]
    public string buildingName = "Nhà Chính";

    [Header("Mảng chứa các Model Cấp 1, 2, 3...")]
    [SerializeField] private GameObject[] visualModels;

    [Header("Cấu hình chi phí nâng cấp (Phần tử 0 là từ Lv1 -> Lv2)")]
    [SerializeField] private UpgradeCost[] upgradeCosts;

    public int CurrentLevel { get; private set; } = 0; // Level hiện tại của công trình
    public int MaxLevel => visualModels != null ? visualModels.Length : 0;

    private GameObject[] instantiatedModels;
    public GameObject[] VisualModels => (instantiatedModels != null && instantiatedModels.Length > 0) ? instantiatedModels : visualModels;

    // Trạng thái kiểm tra xem nhà có đang trong quá trình nâng cấp không
    public bool IsUpgrading { get; private set; } = false;

    // Các sự kiện phục vụ nâng cấp / huấn luyện
    public event System.Action OnUpgradeStart;
    public event System.Action OnUpgradeComplete;
    public event System.Action OnLevelChanged;

    // ====================================================================
    // --- HAI BẠN THÊM ĐOẠN NÀY VÀO ĐỂ QUẢ LÝ CODE CÁC CẤP ĐỘ ---
    [Header("Quản lý Code AI của từng Cấp độ (Kéo các Script tương ứng vào đây)")]
    [SerializeField] private AttackTowerAI[] towerLevelScripts;

    // Cổng public để UIManager hoặc hệ thống khác đứng ngoài lấy danh sách code
    public AttackTowerAI[] TowerLevelScripts => towerLevelScripts;
    // ====================================================================

    [Header("Penta Dev - Quản lý Cấp độ Công trình Dân sự")]
    [SerializeField] private WoodStorage[] woodStorageLevels;
    [SerializeField] private StoneStorage[] stoneStorageLevels;
    [SerializeField] private RiceStorage[] riceStorageLevels;
    [SerializeField] private Kitchen[] kitchenLevels;
    [SerializeField] private House[] houseLevels; // <-- VŨ THÊM DÒNG NÀY VÀO ĐÂY

    public WoodStorage[] WoodStorageLevels => woodStorageLevels;
    public StoneStorage[] StoneStorageLevels => stoneStorageLevels;
    public RiceStorage[] RiceStorageLevels => riceStorageLevels;
    public Kitchen[] KitchenLevels => kitchenLevels;
    public House[] HouseLevels => houseLevels; // <-- VŨ THÊM DÒNG NÀY ĐỂ UI ĐỌC ĐƯỢC
    // ====================================================================

    // Các trường lưu giữ visual gốc phục vụ cơ chế tự tham chiếu không reparent
    private System.Collections.Generic.List<GameObject> originalChildren = new System.Collections.Generic.List<GameObject>();
    private MeshRenderer rootRendererComponent;
    private SkinnedMeshRenderer rootSkinnedRendererComponent;
    private int selfRefIndex = -1;

    // --- CHÈN THÊM ĐOẠN NÀY VÀO ---
    [Header("Mảng chứa các Icon hiển thị trên UI tương ứng từng Cấp")]
    [SerializeField] private Sprite[] buildingIcons;

    public Sprite[] BuildingIcons => buildingIcons;
    // --------------------------------
    // ====================================================================
    // PENTA DEV - PHÂN KHU PHỐI HỢP HỆ THỐNG TÀN TÍCH & SỬA CHỮA
    // ====================================================================

    [Header("Penta Dev - Giao Diện Tàn Tích")]
    [Tooltip("Kéo Model nhà nát (Xác nhà đổ nát) vào đây")]
    [SerializeField] private GameObject ruinedVisualModel;

    [Header("Penta Dev - Chi Phí Sửa Chữa")]
    [SerializeField] private int repairWoodCost = 30;
    [SerializeField] private int repairStoneCost = 30;
    [SerializeField] private float repairDuration = 5f;

    // Property để hệ thống kiểm tra trạng thái công trình xem có đang bị hỏng không
    public bool IsRuined { get; private set; } = false;

    private void Awake()
    {
        if (transform.parent != null && transform.parent.GetComponentInParent<UpgradeableBuilding>() != null)
        {
            // Tắt các script AI và chính nó trên clone này để tránh bắn đạn trùng lặp hoặc lỗi đệ quy hình ảnh
            var attackAI = GetComponent<AttackTowerAI>();
            if (attackAI != null) attackAI.enabled = false;

            var defenceAI = GetComponent<DefenceTowerAI>();
            if (defenceAI != null) defenceAI.enabled = false;

            enabled = false;
        }
    }

    private static UpgradeableBuilding selectedInstance = null;

    public void SelectThisBuilding()
    {
        selectedInstance = this;
        Debug.Log($"[UpgradeableBuilding] Selected {buildingName} for debug upgrade");
    }

    private void OnMouseDown()
    {
        SelectThisBuilding();
    }

    private void SaveOriginalVisuals()
    {
        if (originalChildren.Count > 0 || rootRendererComponent != null || rootSkinnedRendererComponent != null) return;

        rootRendererComponent = GetComponent<MeshRenderer>();
        rootSkinnedRendererComponent = GetComponent<SkinnedMeshRenderer>();

        foreach (Transform child in transform)
        {
            // THÊM DÒNG NÀY VÀO ĐÂY: Bỏ qua model tàn tích không lưu vào danh sách gốc
            if (ruinedVisualModel != null && child.gameObject == ruinedVisualModel) continue;
            // Không tính các visual model khác được kéo sẵn vào (nếu có)
            bool isOtherVisualModel = false;
            if (visualModels != null)
            {
                for (int j = 0; j < visualModels.Length; j++)
                {
                    if (visualModels[j] != gameObject && visualModels[j] == child.gameObject)
                    {
                        isOtherVisualModel = true;
                        break;
                    }
                }
            }
            if (!isOtherVisualModel)
            {
                originalChildren.Add(child.gameObject);
            }
        }
    }

    private void SetOriginalLevelActive(bool active)
    {
        if (rootRendererComponent != null) rootRendererComponent.enabled = active;
        if (rootSkinnedRendererComponent != null) rootSkinnedRendererComponent.enabled = active;

        for (int i = 0; i < originalChildren.Count; i++)
        {
            if (originalChildren[i] != null)
            {
                originalChildren[i].SetActive(active);
            }
        }
    }

    private void UpdateFirePointForLevel()
    {
        var attackAI = GetComponent<AttackTowerAI>();
        if (attackAI == null) return;

        Transform fp = null;
        if (CurrentLevel == selfRefIndex)
        {
            // Tìm trong các visual gốc ban đầu
            for (int i = 0; i < originalChildren.Count; i++)
            {
                if (originalChildren[i] != null)
                {
                    fp = FindFirePointRecursive(originalChildren[i].transform);
                    if (fp != null) break;
                }
            }
        }
        else
        {
            if (instantiatedModels != null && CurrentLevel >= 0 && CurrentLevel < instantiatedModels.Length)
            {
                GameObject activeModel = instantiatedModels[CurrentLevel];
                if (activeModel != null)
                {
                    fp = FindFirePointRecursive(activeModel.transform);
                }
            }
        }

        if (fp != null)
        {
            attackAI.firePoint = fp;
            Debug.Log($"[UpgradeableBuilding] Updated attackAI.firePoint to: {fp.name} on Level {CurrentLevel + 1}");
        }
    }

    private Transform FindFirePointRecursive(Transform parent)
    {
        string nameLower = parent.name.ToLower();
        if (nameLower.Contains("firepoint") || nameLower.Contains("muzzle") || nameLower.Contains("spawn") || nameLower.Contains("shoot"))
        {
            return parent;
        }

        foreach (Transform child in parent)
        {
            Transform found = FindFirePointRecursive(child);
            if (found != null) return found;
        }

        return null;
    }

    public void InitializeModels()
    {
        if (instantiatedModels != null) return;
        if (visualModels == null) return;

        instantiatedModels = new GameObject[visualModels.Length];

        // 1. Kiểm tra xem có phần tử nào là tự tham chiếu (self-reference) tới chính gameObject này hay không
        selfRefIndex = -1;
        for (int i = 0; i < visualModels.Length; i++)
        {
            if (visualModels[i] == gameObject)
            {
                selfRefIndex = i;
                break;
            }
        }

        if (selfRefIndex != -1)
        {
            // Lưu lại các children gốc hiện tại trước khi sinh bất cứ model mới nào con của nó
            SaveOriginalVisuals();
            instantiatedModels[selfRefIndex] = gameObject;
        }

        // 2. Khởi tạo các phần tử còn lại từ Prefab hoặc Object con khác
        for (int i = 0; i < visualModels.Length; i++)
        {
            if (i == selfRefIndex) continue;

            GameObject modelSource = visualModels[i];
            if (modelSource == null) continue;

            // Kiểm tra xem modelSource có phải là Prefab ngoài Project hay không (scene của nó không hợp lệ)
            if (!modelSource.scene.IsValid() || string.IsNullOrEmpty(modelSource.scene.name))
            {
                // Instantiate thành gameobject con của building
                GameObject newInstance = Instantiate(modelSource, transform.position, transform.rotation, transform);
                newInstance.name = modelSource.name;
                instantiatedModels[i] = newInstance;
                newInstance.SetActive(i == CurrentLevel);
            }
            else
            {
                // Sử dụng luôn gameobject có sẵn trong Scene
                instantiatedModels[i] = modelSource;
                modelSource.SetActive(i == CurrentLevel);
            }
        }

        // 3. Nếu không có tự tham chiếu, thực hiện ẩn các MeshRenderer gốc ban đầu trên parent tránh chồng lấn
        if (selfRefIndex == -1)
        {
            MeshRenderer rootRenderer = GetComponent<MeshRenderer>();
            if (rootRenderer != null)
            {
                rootRenderer.enabled = false;
            }
            SkinnedMeshRenderer rootSkinnedRenderer = GetComponent<SkinnedMeshRenderer>();
            if (rootSkinnedRenderer != null)
            {
                rootSkinnedRenderer.enabled = false;
            }

            foreach (Transform child in transform)
            {
                // THÊM DÒNG NÀY VÀO ĐÂY: Nếu là nhà nát thì bỏ qua, không được tắt MeshRenderer
                if (ruinedVisualModel != null && child.gameObject == ruinedVisualModel) continue;

                bool isVisualModel = false;
                foreach (var im in instantiatedModels)
                {
                    if (im == child.gameObject)
                    {
                        isVisualModel = true;
                        break;
                    }
                }
                if (!isVisualModel)
                {
                    foreach (var mr in child.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        mr.enabled = false;
                    }
                    foreach (var smr in child.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        smr.enabled = false;
                    }
                }
            }
        }
    }

    private void Start()
    {
        Debug.Log($"[UpgradeableBuilding Debug] Start called on '{gameObject.name}'");
        InitializeModels();
        UpdateVisualModel();

        if (visualModels != null)
        {
            Debug.Log($"[UpgradeableBuilding Debug] visualModels count: {visualModels.Length}");
            for (int i = 0; i < visualModels.Length; i++)
            {
                var vm = visualModels[i];
                Debug.Log($"  - visualModels[{i}]: {(vm != null ? vm.name : "null")} (IsSceneObject: {(vm != null ? vm.scene.IsValid().ToString() : "N/A")})");
            }
        }
        else
        {
            Debug.Log("[UpgradeableBuilding Debug] visualModels is null!");
        }

        if (instantiatedModels != null)
        {
            Debug.Log($"[UpgradeableBuilding Debug] instantiatedModels count: {instantiatedModels.Length}");
            for (int i = 0; i < instantiatedModels.Length; i++)
            {
                var im = instantiatedModels[i];
                Debug.Log($"  - instantiatedModels[{i}]: {(im != null ? im.name : "null")} (ActiveSelf: {(im != null ? im.activeSelf.ToString() : "N/A")})");
            }
        }
        else
        {
            Debug.Log("[UpgradeableBuilding Debug] instantiatedModels is null!");
        }

        // Tự động gán ClickHelper cho tất cả các Collider con để bắt sự kiện click
        foreach (Collider col in GetComponentsInChildren<Collider>(true))
        {
            if (col.gameObject.GetComponent<ClickHelper>() == null)
            {
                ClickHelper helper = col.gameObject.AddComponent<ClickHelper>();
                helper.parentBuilding = this;
            }
        }

        // Nếu căn nhà này được đánh dấu là CẦN XÂY DỰNG BAN ĐẦU
        if (isInitialBuildNeeded)
        {
            // Kích hoạt trạng thái nâng cấp giả lập để UI bắt đầu đếm số và chạy VFX/SFX
            IsUpgrading = true;
            StartCoroutine(UpgradeRoutine(initialBuildDuration));
        }

        UpdateCivilianBuildingData();
    }

    // Hàm lấy chi phí cần thiết để lên cấp tiếp theo
    public UpgradeCost GetNextUpgradeCost()
    {
        if (upgradeCosts != null && CurrentLevel < upgradeCosts.Length)
        {
            return upgradeCosts[CurrentLevel];
        }
        // Trả về mặc định nếu đạt cấp tối đa
        return new UpgradeCost { woodCost = 0, stoneCost = 0, foodCost = 0, upgradeDuration = 0f };
    }

    /// <summary>
    /// Kích hoạt tiến trình đếm ngược nâng cấp bằng Coroutine
    /// </summary>
    public void StartUpgradeProcess()
    {
        if (IsUpgrading || CurrentLevel >= MaxLevel - 1) return;

        UpgradeCost nextCost = GetNextUpgradeCost();
        float duration = nextCost.upgradeDuration;

        // Nếu là nhà lính (BarracksMelee, BarracksArcher, BarracksSpear) hoặc nếu duration chưa được thiết lập hợp lệ, mặc định ép về 5 giây
        if (buildingType == BuildingType.BarracksMelee || 
            buildingType == BuildingType.BarracksArcher || 
            buildingType == BuildingType.BarracksSpear || 
            duration <= 0f)
        {
            duration = 5f;
        }

        StartCoroutine(UpgradeRoutine(duration));
    }

    private IEnumerator UpgradeRoutine(float duration)
    {
        IsUpgrading = true;
        OnUpgradeStart?.Invoke();
        float timer = 0f;

        // Nếu panel nâng cấp của nhà này đang mở trên UI, hiển thị slider/text thời gian
        if (UIManager.Ins != null)
        {
            UIManager.Ins.UpdateUpgradeProgress(0f, duration);
        }

        // Vòng lặp đếm ngược thời gian nâng cấp theo thời gian thực
        while (timer < duration)
        {
            timer += Time.deltaTime;

            // Cập nhật giá trị hiển thị lên UI liên tục mỗi khung hình
            if (UIManager.Ins != null)
            {
                UIManager.Ins.UpdateUpgradeProgress(timer, duration);
            }
            yield return null;
        }

        // ====================================================================
        // --- PHÂN TÁCH LUỒNG XỬ LÝ KHI HẾT THỜI GIAN (PENTA DEV - VŨ) ---
        // ====================================================================
        if (isInitialBuildNeeded)
        {
            isInitialBuildNeeded = false; // TẮT TÍCH VĨNH VIỄN: Xác nhận đã xây dựng xong!
            IsUpgrading = false;
            OnUpgradeComplete?.Invoke();
            OnLevelChanged?.Invoke();

            // Kích hoạt hiệu ứng hoàn thành (Aura quét dọc thân nhà) từ BuildingProgressBarUI
            var targetUI = BuildingProgressBridge.GetUI(this);
            if (targetUI != null)
            {
                targetUI.HandleCompleteSequence();
            }

            // >>> THÊM MỚI: Báo cho BuildingCtrl biết công trình đã xây xong thật sự
            var buildingCtrl = GetComponent<BuildingCtrl>();
            if (buildingCtrl != null)
            {
                buildingCtrl.AddProgress(1f);
            }

            Debug.Log($"[Penta Dev] 🏠 Công trình {buildingName} đã hoàn thành xây dựng lần đầu tiên thành công!");
        }
        else
        {
            IsUpgrading = false;
            OnUpgradeComplete?.Invoke();
            ExecuteLevelUp();
        }
        // ====================================================================

        // TẮT TEXT VÀ SLIDER THỜI GIAN KHI NÂNG CẤP/XÂY DỰNG XONG
        if (UIManager.Ins != null)
        {
            UIManager.Ins.HideUpgradeProgress();
            UIManager.Ins.RefreshUpgradePanel(this);
        }
    }



    /// <summary>
    /// Được gọi tự động từ hàm OnDeath() của HPTower khi công trình bị hết máu
    /// </summary>
    public void TriggerDestructionSequence()
    {
        IsRuined = true;

        // 1. Ẩn Model đồ họa cấp độ hiện tại của nhà đi
        SetActiveModel(CurrentLevel, false);

        // 2. Bật Model tàn tích đổ nát lên
        if (ruinedVisualModel != null)
        {
            ruinedVisualModel.SetActive(true);
        }

        // 3. Tắt hoạt động AI tháp phòng thủ (nếu có) để ngừng bắn quái
        ToggleBuildingLogic(false);

        // 4. KIỂM TRA ĐIỀU KIỆN NHÀ CHÍNH SẬP ĐỂ HIỆN BẢNG TỔNG KẾT (GAME OVER)
        if (buildingName.Contains("Nhà Chính"))
        {
            Debug.LogError("[Penta Dev] 🔥 NHÀ CHÍNH ĐÃ BỊ PHÁ HỦY! Kích hoạt bảng tổng kết chiến dịch...");
            // Thêm luồng gọi bảng UI tổng kết của nhóm Vũ tại đây, ví dụ:
            // if (UIManager.Ins != null) UIManager.Ins.ShowSummaryPanel();
        }
    }


    /// <summary>
    /// Hàm ra lệnh bắt đầu sửa chữa (Được gọi từ nút Sửa Chữa trên giao diện UI)
    /// </summary>
    public void StartRepair()
    {
        if (!IsRuined || IsUpgrading) return; // Đang bận nâng cấp hoặc nhà chưa hỏng thì bỏ qua

        // CHẶN TẬN GỐC: trừ tài nguyên sửa chữa tại đây, TRƯỚC khi cho phép đếm giờ sửa chữa.
        if (JsonDataManager.Ins == null)
        {
            Debug.LogWarning($"[UpgradeableBuilding] Không tìm thấy JsonDataManager.Ins — huỷ sửa chữa {buildingName}.");
            return;
        }

        bool spent = JsonDataManager.Ins.TrySpendCombined(
            woodCost: repairWoodCost,
            stoneCost: repairStoneCost);

        if (!spent)
        {
            Debug.LogWarning($"[UpgradeableBuilding] Không đủ tài nguyên để sửa chữa {buildingName} (cần Gỗ:{repairWoodCost} Đá:{repairStoneCost}).");
            return;
        }

        StartCoroutine(RepairRoutine());
    }

    private IEnumerator RepairRoutine()
    {
        // Sử dụng cầu nối BuildingProgressBridge có sẵn của Vũ để tìm đúng UI bar trên đầu nhà
        var targetProgressUI = BuildingProgressBridge.GetUI(this);
        float timer = 0f;

        // Vòng lặp chạy thanh tiến trình đếm ngược thời gian sửa chữa y hệt luồng nâng cấp
        while (timer < repairDuration)
        {
            timer += Time.deltaTime;
            if (targetProgressUI != null)
            {
                // Đẩy số giây chạy lên slider và text
                targetProgressUI.UpdateProgress(timer, repairDuration);
            }
            yield return null;
        }

        // --- HOÀN THÀNH TIẾN TRÌNH SỬA CHỮA ---
        IsRuined = false;

        // 1. Gọi HPTower hồi lại toàn bộ máu và thiết lập lại trạng thái sinh tồn
        HPTower hpComponent = GetComponent<HPTower>();
        if (hpComponent != null)
        {
            hpComponent.ResetHealth();
        }

        // 2. Ẩn model xác nhà nát đi
        if (ruinedVisualModel != null) ruinedVisualModel.SetActive(false);

        // 3. Hiển thị lại Model nhà nguyên bản theo đúng Cấp độ hiện tại
        UpdateVisualModel();

        // 4. Kích hoạt bật lại các đoạn code AI bắn đạn tháp phòng thủ
        ToggleBuildingLogic(true);

        // 5. Chạy hiệu ứng hào quang hoàn thành (Aura VFX) và ẩn thanh đếm
        if (targetProgressUI != null)
        {
            targetProgressUI.HandleCompleteSequence();
        }

        // Làm mới lại bảng thông tin nâng cấp trên UI
        if (UIManager.Ins != null)
        {
            UIManager.Ins.RefreshUpgradePanel(this);
        }

        Debug.Log($"[Penta Dev] 🛠️ Công trình {buildingName} đã được sửa chữa và phục hồi trạng thái hoạt động!");
    }

    /// <summary>
    /// Hàm phụ trợ Tắt/Bật code AI hoạt động của tháp
    /// </summary>
    private void ToggleBuildingLogic(bool active)
    {
        if (towerLevelScripts != null)
        {
            foreach (var towerScript in towerLevelScripts)
            {
                if (towerScript != null) towerScript.enabled = active;
            }
        }
    }

    /// <summary>
    /// Thực hiện thay đổi cấp độ và model thực tế
    /// </summary>
    [ContextMenu("⚡ Nâng cấp Tháp này")]
    public void ExecuteLevelUp()
    {
        if (CurrentLevel < MaxLevel - 1)
        {
            // Ẩn model hiện tại
            SetActiveModel(CurrentLevel, false);

            CurrentLevel++; // Tăng cấp độ hiện tại lên

            // Cập nhật chỉ số và tự gọi SetupLevel cho công trình dân sự mới
            UpdateCivilianBuildingData();

            // Làm mới Panel nâng cấp để đẩy text lên UI ngay lập tức
            if (UIManager.Ins != null)
            {
                UIManager.Ins.RefreshUpgradePanel(this);
            }

            Debug.Log($"[UpgradeableBuilding] {buildingName} đã nâng lên Level {CurrentLevel + 1}");

            // Hiện model mới
            SetActiveModel(CurrentLevel, true);

            Debug.Log($"[{buildingName}] Đã hoàn tất nâng cấp lên Level {CurrentLevel + 1}");
            OnLevelChanged?.Invoke();
        }
    }

    [ContextMenu("🔄 Reset level về 1")]
    public void ResetLevel()
    {
        SetActiveModel(CurrentLevel, false);
        CurrentLevel = 0;
        SetActiveModel(CurrentLevel, true);
        UpdateCivilianBuildingData();
        Debug.Log($"[{buildingName}] Đã reset về Level 1");
        OnLevelChanged?.Invoke();
    }

    private void SetActiveModel(int index, bool active)
    {
        InitializeModels();
        if (instantiatedModels == null || index < 0 || index >= instantiatedModels.Length) return;

        if (index == selfRefIndex)
        {
            SetOriginalLevelActive(active);
        }
        else
        {
            if (instantiatedModels[index] != null)
                instantiatedModels[index].SetActive(active);
        }

        if (active)
        {
            UpdateFirePointForLevel();
        }
    }

    public void UpdateVisualModel()
    {
        InitializeModels();
        if (instantiatedModels == null) return;
        for (int i = 0; i < instantiatedModels.Length; i++)
        {
            if (i == selfRefIndex)
            {
                SetOriginalLevelActive(i == CurrentLevel);
            }
            else
            {
                if (instantiatedModels[i] != null)
                    instantiatedModels[i].SetActive(i == CurrentLevel);
            }
        }

        UpdateFirePointForLevel();
    }

    private void OnGUI()
    {
        if (selectedInstance == this)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 18;
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.normal.textColor = Color.white;

            string btnText = $"⚡ NÂNG CẤP: {buildingName} (Lv {CurrentLevel + 1} -> {CurrentLevel + 2})";
            bool isMax = CurrentLevel >= MaxLevel - 1;
            if (isMax)
            {
                btnText = $"🔄 RESET VỀ LEVEL 1 ({buildingName})";
            }

            // Vẽ background box
            GUI.Box(new Rect(10, 10, 480, 80), $"Bảng Nâng Cấp Nhanh - {buildingName} (Đang chọn)");

            if (GUI.Button(new Rect(20, 35, 390, 45), btnText, buttonStyle))
            {
                if (!isMax)
                {
                    ExecuteLevelUp();
                }
                else
                {
                    ResetLevel();
                }
            }

            // Nút close
            if (GUI.Button(new Rect(420, 35, 60, 45), "X", buttonStyle))
            {
                selectedInstance = null;
            }
        }
    }

    // --- KHU VỰC ĐỒNG BỘ DÂN SỰ CỦA VỦ VÀ ĐĂNG (GIỮ LẠI BẢN GETCOMPONENTINCHILDREN TỐI ƯU) ---
    private void UpdateCivilianBuildingData()
    {
        // Khi nhà nâng cấp, Model mới được bật lên. Chúng ta cần lấy đúng Script dân sự nằm trên Model đó hoặc trên chính Object này.
        switch (buildingType)
        {
            case BuildingType.WoodCutter: // Sửa lại đúng tên Enum loại kho gỗ của các bạn
                WoodStorage ws = GetComponentInChildren<WoodStorage>();
                if (ws != null) ws.SetupLevel(CurrentLevel);
                break;

            case BuildingType.StoneStorage:
                StoneStorage ss = GetComponentInChildren<StoneStorage>();
                if (ss != null) ss.SetupLevel(CurrentLevel);
                break;

            case BuildingType.FoodStorage: // Sửa lại đúng tên Enum loại kho lúa của các bạn
                RiceStorage rs = GetComponentInChildren<RiceStorage>();
                if (rs != null) rs.SetupLevel(CurrentLevel);
                break;

            case BuildingType.Kitchen:
                Kitchen kc = GetComponentInChildren<Kitchen>();
                if (kc != null) kc.SetupLevel(CurrentLevel);
                break;
            case BuildingType.House: // <-- THÊM LOGIC ĐỒNG BỘ CHO NHÀ WORKER
                House hs = GetComponentInChildren<House>();
                // Nếu các file House của bạn có viết hàm SetupLevel(level) giống WoodStorage thì gọi ở đây,
                // hoặc tạm thời giữ để cập nhật model visual khi nâng cấp thành công.
                break;
        }
    }

    /// <summary>
    /// Thiết lập lại cấp độ của công trình khi tải dữ liệu từ file JSON.
    /// </summary>
    public void LoadLevel(int level)
    {
        StopAllCoroutines(); // Dừng các tiến trình xây dựng hoặc nâng cấp đang chạy
        IsUpgrading = false;
        isInitialBuildNeeded = false; // Đã tải dữ liệu từ Save -> Xác nhận đã được xây xong từ trước

        SetActiveModel(CurrentLevel, false);
        CurrentLevel = Mathf.Clamp(level, 0, MaxLevel - 1);
        SetActiveModel(CurrentLevel, true);

        UpdateCivilianBuildingData();
        OnLevelChanged?.Invoke();
    }
}

// Lớp trợ giúp bắt sự kiện click cho các collider con
public class ClickHelper : MonoBehaviour
{
    public UpgradeableBuilding parentBuilding;
    private void OnMouseDown()
    {
        if (parentBuilding != null)
        {
            parentBuilding.SelectThisBuilding();
        }
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(UpgradeableBuilding))]
public class UpgradeableBuildingEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        UpgradeableBuilding building = (UpgradeableBuilding)target;

        GUILayout.Space(15);
        GUILayout.Label("⚡ BẢNG ĐIỀU KHIỂN NÂNG CẤP NHANH", UnityEditor.EditorStyles.boldLabel);

        // Hiển thị thông tin level hiện tại
        UnityEditor.EditorGUILayout.HelpBox($"Cấp độ hiện tại: Level {building.CurrentLevel + 1} / {building.MaxLevel}", UnityEditor.MessageType.Info);

        if (GUILayout.Button("⚡ NÂNG CẤP NGAY", GUILayout.Height(40)))
        {
            if (Application.isPlaying)
            {
                building.ExecuteLevelUp();
            }
            else
            {
                UnityEditor.EditorUtility.DisplayDialog("Thông báo", "Vui lòng bấm nút PLAY (Chạy game) trên thanh công cụ Unity trước khi sử dụng nút này!", "OK");
            }
        }

        if (GUILayout.Button("🔄 RESET VỀ LEVEL 1", GUILayout.Height(30)))
        {
            if (Application.isPlaying)
            {
                building.ResetLevel();
            }
            else
            {
                UnityEditor.EditorUtility.DisplayDialog("Thông báo", "Vui lòng bấm nút PLAY (Chạy game) trên thanh công cụ Unity trước khi sử dụng nút này!", "OK");
            }
        }
    }
}
#endif