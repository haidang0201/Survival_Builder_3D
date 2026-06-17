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

    public WoodStorage[] WoodStorageLevels => woodStorageLevels;
    public StoneStorage[] StoneStorageLevels => stoneStorageLevels;
    public RiceStorage[] RiceStorageLevels => riceStorageLevels;
    public Kitchen[] KitchenLevels => kitchenLevels;
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
        StartCoroutine(UpgradeRoutine(nextCost.upgradeDuration));
    }

    private IEnumerator UpgradeRoutine(float duration)
    {
        IsUpgrading = true;
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

        // Sau khi chạy hết thời gian chờ -> Tiến hành đổi cấp bậc hình ảnh
        ExecuteLevelUp();

        IsUpgrading = false;

        // TẮT TEXT VÀ SLIDER THỜI GIAN KHI NÂNG CẤP XONG
        if (UIManager.Ins != null)
        {
            UIManager.Ins.HideUpgradeProgress();
            UIManager.Ins.RefreshUpgradePanel(this);
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
        }
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