using UnityEngine;
using UnityEngine.UI;
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

    [Header("Upgrade World Space UI")]
    [SerializeField] private GameObject upgradeSliderPrefab; // Prefab slider tùy chọn từ Editor
    [SerializeField] private Vector3 sliderOffset = new Vector3(0f, 4f, 0f); // Khoảng cách offset hiển thị slider trên đầu

    private GameObject activeWorldSliderInstance;
    private UnityEngine.UI.Slider activeWorldSlider;

    // Các trường lưu giữ visual gốc phục vụ cơ chế tự tham chiếu không reparent
    private System.Collections.Generic.List<GameObject> originalChildren = new System.Collections.Generic.List<GameObject>();
    private MeshRenderer rootRendererComponent;
    private SkinnedMeshRenderer rootSkinnedRendererComponent;
    private int selfRefIndex = -1;

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
    }

    // Hàm lấy chi phí cần thiết để lên cấp tiếp theo
    public UpgradeCost GetNextUpgradeCost()
    {
        if (CurrentLevel < upgradeCosts.Length)
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
        
        // Nếu nâng cấp từ Level 1 lên Level 2 (CurrentLevel = 0 lên 1)
        if (CurrentLevel == 0)
        {
            duration = 5f;
        }
        else if (duration <= 0f)
        {
            duration = 5f; // Dự phòng mặc định 5s cho các cấp độ khác
        }

        StartCoroutine(UpgradeRoutine(duration));
    }

    private IEnumerator UpgradeRoutine(float duration)
    {
        IsUpgrading = true;
        float timer = 0f;

        // Tạo thanh slider tiến trình nâng cấp trên đầu công trình
        CreateWorldUpgradeSlider();

        // Nếu panel nâng cấp của nhà này đang mở trên UI, hiển thị slider/text thời gian
        if (UIManager.Ins != null)
        {
            UIManager.Ins.UpdateUpgradeProgress(0f, duration);
        }

        // Vòng lặp đếm ngược thời gian nâng cấp theo thời gian thực
        while (timer < duration)
        {
            timer += Time.deltaTime;
            
            // Cập nhật giá trị hiển thị lên slider thế giới (World Space UI)
            if (activeWorldSlider != null)
            {
                activeWorldSlider.value = Mathf.Clamp01(timer / duration);
            }

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

        // Hủy thanh slider tiến trình sau khi nâng cấp xong
        if (activeWorldSliderInstance != null)
        {
            Destroy(activeWorldSliderInstance);
        }

        // TẮT TEXT VÀ SLIDER THỜI GIAN KHI NÂNG CẤP XONG
        if (UIManager.Ins != null)
        {
            UIManager.Ins.HideUpgradeProgress();
            UIManager.Ins.RefreshUpgradePanel(this);
        }
    }

    private void CreateWorldUpgradeSlider()
    {
        if (activeWorldSliderInstance != null)
        {
            Destroy(activeWorldSliderInstance);
        }

        if (upgradeSliderPrefab != null)
        {
            activeWorldSliderInstance = Instantiate(upgradeSliderPrefab, transform.position + sliderOffset, Quaternion.identity, transform);
            activeWorldSlider = activeWorldSliderInstance.GetComponentInChildren<UnityEngine.UI.Slider>();
            return;
        }

        // Tạo Canvas World Space động
        GameObject canvasObj = new GameObject("UpgradeProgressCanvas");
        canvasObj.transform.SetParent(transform, false);
        canvasObj.transform.position = transform.position + sliderOffset;

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // Tạo RectTransform cho Canvas
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(150f, 20f);
        canvasRect.localScale = new Vector3(0.015f, 0.015f, 0.015f);

        // Tạo Background Image
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.6f);

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.localScale = Vector3.one;

        // Tạo Slider GameObject
        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Slider slider = sliderObj.AddComponent<UnityEngine.UI.Slider>();

        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.05f, 0.15f);
        sliderRect.anchorMax = new Vector2(0.95f, 0.85f);
        sliderRect.sizeDelta = Vector2.zero;
        sliderRect.localScale = Vector3.one;

        // Tạo Fill Area
        GameObject fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;
        fillAreaRect.localScale = Vector3.one;

        // Tạo Fill Image
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.8f, 0.3f, 1f); // Màu xanh lục tươi sáng

        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.sizeDelta = Vector2.zero;
        fillRect.localScale = Vector3.one;

        // Cấu hình Slider
        slider.targetGraphic = bgImage;
        slider.fillRect = fillRect;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;

        canvas.sortingOrder = 100; // Hiển thị trên các vật thể khác

        activeWorldSliderInstance = canvasObj;
        activeWorldSlider = slider;

        // Đồng bộ Layer của toàn bộ Canvas với công trình để Camera hiển thị chính xác
        SetLayerRecursive(canvasObj, gameObject.layer);
    }

    private void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }

    private void LateUpdate()
    {
        if (activeWorldSliderInstance != null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                activeWorldSliderInstance.transform.LookAt(activeWorldSliderInstance.transform.position + mainCam.transform.rotation * Vector3.forward,
                    mainCam.transform.rotation * Vector3.up);
            }
        }
    }

    private void OnDestroy()
    {
        if (activeWorldSliderInstance != null)
        {
            Destroy(activeWorldSliderInstance);
        }
    }

    /// <summary>
    /// Thực hiện thay đổi cấp độ và model thực tế
    /// </summary>
    [ContextMenu("⚡ Nâng cấp Tháp này")]
    public void ExecuteLevelUp()
    {
        if (activeWorldSliderInstance != null)
        {
            Destroy(activeWorldSliderInstance);
        }

        if (CurrentLevel < MaxLevel - 1)
        {
            // Ẩn model hiện tại
            SetActiveModel(CurrentLevel, false);

            // Tăng level
            CurrentLevel++;

            // Hiện model mới
            SetActiveModel(CurrentLevel, true);

            Debug.Log($"[{buildingName}] Đã hoàn tất nâng cấp lên Level {CurrentLevel + 1}");
        }
    }

    [ContextMenu("🔄 Reset level về 1")]
    public void ResetLevel()
    {
        if (activeWorldSliderInstance != null)
        {
            Destroy(activeWorldSliderInstance);
        }

        SetActiveModel(CurrentLevel, false);
        CurrentLevel = 0;
        SetActiveModel(CurrentLevel, true);
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

            if (IsUpgrading)
            {
                btnText = $"⏳ ĐANG NÂNG CẤP {buildingName}...";
            }
            else if (isMax)
            {
                btnText = $"🔄 RESET VỀ LEVEL 1 ({buildingName})";
            }

            // Vẽ background box
            GUI.Box(new Rect(10, 10, 480, 80), $"Bảng Nâng Cấp Nhanh - {buildingName} (Đang chọn)");

            if (GUI.Button(new Rect(20, 35, 390, 45), btnText, buttonStyle))
            {
                if (!IsUpgrading)
                {
                    if (!isMax)
                    {
                        StartUpgradeProcess();
                    }
                    else
                    {
                        ResetLevel();
                    }
                }
            }

            // Nút close
            if (GUI.Button(new Rect(420, 35, 60, 45), "X", buttonStyle))
            {
                selectedInstance = null;
            }
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
        string statusText = building.IsUpgrading ? $"Đang nâng cấp..." : $"Cấp độ hiện tại: Level {building.CurrentLevel + 1} / {building.MaxLevel}";
        UnityEditor.EditorGUILayout.HelpBox(statusText, UnityEditor.MessageType.Info);

        if (building.IsUpgrading)
        {
            UnityEditor.EditorGUILayout.HelpBox("Đang trong quá trình nâng cấp, vui lòng chờ...", UnityEditor.MessageType.Warning);
        }

        if (GUILayout.Button(building.IsUpgrading ? "⏳ ĐANG NÂNG CẤP..." : "⚡ BẮT ĐẦU NÂNG CẤP (5s)", GUILayout.Height(40)))
        {
            if (Application.isPlaying)
            {
                if (!building.IsUpgrading)
                {
                    building.StartUpgradeProcess();
                }
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