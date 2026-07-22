using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SpawnSoldier : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject soldierPrefab;
    [SerializeField] private float spawnRadius = 5f;

    [Header("Upgrade Settings")]
    [SerializeField] private int currentLevel = 1;

    [Header("Test Settings")]
    [SerializeField] private float testDuration = 5f;

    // Danh sách lưu các lính đã spawn để có thể xóa khi nâng cấp
    private List<GameObject> spawnedSoldiers = new List<GameObject>();
    private UpgradeableBuilding upgradeableBuilding;
    private bool isOnMainBuildingObject = false;

    [Header("Hologram Settings")]
    [SerializeField] private Material hologramMaterial;
    [SerializeField] private Color hologramColor = new Color(0f, 0.7f, 1f, 0.35f);
    private bool spawnedHolograms = false;
    private List<GameObject> spawnedHologramsList = new List<GameObject>();
    private Material dynamicHologramMaterial;
    private bool isTesting = false;
    private Coroutine hologramAnimationCoroutine;

    public float TestDuration => testDuration;

    void Awake()
    {
        // Tự động tìm component UpgradeableBuilding trên cùng Object hoặc ở Object cha
        upgradeableBuilding = GetComponent<UpgradeableBuilding>();
        if (upgradeableBuilding != null)
        {
            isOnMainBuildingObject = true;
        }
        else
        {
            upgradeableBuilding = GetComponentInParent<UpgradeableBuilding>();
            isOnMainBuildingObject = false;
        }
    }

    void OnEnable()
    {
        if (upgradeableBuilding != null)
        {
            upgradeableBuilding.OnUpgradeStart += HandleUpgradeStart;
            upgradeableBuilding.OnUpgradeComplete += HandleUpgradeComplete;
            upgradeableBuilding.OnLevelChanged += HandleLevelChanged;

            if (upgradeableBuilding.IsUpgrading && !upgradeableBuilding.IsInitialBuildNeeded)
            {
                HandleUpgradeStart();
            }
            else
            {
                SyncLevel();
            }
        }
        else
        {
            // Spawn số lượng lính tương ứng với Level hiện tại (nếu ko có building)
            int initialCount = GetMaxSoldiersForLevel(currentLevel);
            SpawnSoldiers(initialCount);
        }
    }

    void OnDisable()
    {
        if (upgradeableBuilding != null)
        {
            upgradeableBuilding.OnUpgradeStart -= HandleUpgradeStart;
            upgradeableBuilding.OnUpgradeComplete -= HandleUpgradeComplete;
            upgradeableBuilding.OnLevelChanged -= HandleLevelChanged;
        }

        // Khi Spawner bị tắt (do nâng cấp tắt model con hoặc bị hủy), xóa toàn bộ lính cũ
        if (spawnedSoldiers != null && spawnedSoldiers.Count > 0)
        {
            ClearSpawnedSoldiers();
        }
        ClearHolograms();
    }

    private void SyncLevel()
    {
        if (upgradeableBuilding != null)
        {
            if (isOnMainBuildingObject)
            {
                currentLevel = upgradeableBuilding.CurrentLevel + 1;
            }
            else
            {
                int activeLevel = upgradeableBuilding.CurrentLevel + 1;
                // Chỉ sinh lính nếu cấp độ của script này trùng khớp với cấp độ thực tế của công trình
                if (currentLevel != activeLevel)
                {
                    return;
                }
            }
        }

        int count = GetMaxSoldiersForLevel(currentLevel);
        SpawnSoldiers(count);
    }

    private void HandleUpgradeStart()
    {
        if (isTesting) return;
        if (upgradeableBuilding == null) return;
        if (upgradeableBuilding.IsInitialBuildNeeded) return;

        // Chỉ sinh hologram nếu cấp độ hiện tại của spawner khớp với cấp độ đang hoạt động của nhà
        int activeLevel = upgradeableBuilding.CurrentLevel + 1;
        if (currentLevel == activeLevel)
        {
            // Khi bắt đầu nâng cấp, dọn dẹp lính thực cũ trước để lấy chỗ cho hologram
            ClearSpawnedSoldiers();

            // Số lượng hologram spawn bằng đúng level của công trình hiện tại
            int count = GetMaxSoldiersForLevel(currentLevel);

            SpawnHologramSoldiers(count);
            StartHologramAnimationCoroutine();
        }
    }

    private void HandleUpgradeComplete()
    {
        if (isTesting) return;
        ClearHolograms();
    }

    private void HandleLevelChanged()
    {
        if (isOnMainBuildingObject)
        {
            int targetLevel = upgradeableBuilding.CurrentLevel + 1;
            if (currentLevel != targetLevel)
            {
                Debug.Log($"[SpawnSoldier] Đồng bộ nâng cấp: Level thay đổi từ {currentLevel} -> {targetLevel}. Tiến hành xóa lính cũ/hologram và spawn lính mới.");
                ClearHolograms();
                ClearSpawnedSoldiers();
                currentLevel = targetLevel;
                int newCount = GetMaxSoldiersForLevel(currentLevel);
                SpawnSoldiers(newCount);
            }
        }
    }

    private void StartHologramAnimationCoroutine()
    {
        StopHologramAnimationCoroutine();
        hologramAnimationCoroutine = StartCoroutine(HologramAnimationRoutine());
    }

    private void StopHologramAnimationCoroutine()
    {
        if (hologramAnimationCoroutine != null)
        {
            StopCoroutine(hologramAnimationCoroutine);
            hologramAnimationCoroutine = null;
        }
    }

    private System.Collections.IEnumerator HologramAnimationRoutine()
    {
        while (spawnedHolograms)
        {
            UpdateHologramAnimations();
            yield return null;
        }
    }

    // Hàm ép lính quay về hoạt ảnh Idle (Dùng cho lính thật khi đứng yên)
    private void PlayIdleAnimationOnAnimator(Animator anim)
    {
        if (anim == null) return;

        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == "IsTrain" && param.type == AnimatorControllerParameterType.Bool)
            {
                anim.SetBool("IsTrain", false);
            }
            if (param.name == "IsAttack" && param.type == AnimatorControllerParameterType.Bool)
            {
                anim.SetBool("IsAttack", false);
            }
            if (param.name == "IsShoot" && param.type == AnimatorControllerParameterType.Bool)
            {
                anim.SetBool("IsShoot", false);
            }
        }

        string[] idleStateNames = new string[] { "Idle", "IdleArcher", "IdleWalker", "Idle_Attack", "IdleCanonLv1", "IdleCanonLv2", "IdleCanonLv3" };
        bool stateFound = false;

        foreach (string stateName in idleStateNames)
        {
            int hash = Animator.StringToHash(stateName);
            if (anim.HasState(0, hash))
            {
                anim.Play(hash, 0, 0f);
                stateFound = true;
                break;
            }
        }

        if (!stateFound)
        {
            anim.Play(0, 0, 0f);
        }
    }

    // Hàm ép lính chuyển sang hoạt ảnh Train / Attack (Dùng cho lính Hologram khi đếm ngược)
    private void PlayTrainAnimationOnAnimator(Animator anim, bool isInitial = false)
    {
        if (anim == null) return;

        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == "IsTrain" && param.type == AnimatorControllerParameterType.Bool)
            {
                anim.SetBool("IsTrain", true);
            }
        }

        string[] trainStateNames = new string[] { "Train", "ArcherAttackLv1", "Attack", "Chem", "Shoot", "AttackCanonLv1" };
        bool stateFound = false;

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

        foreach (string stateName in trainStateNames)
        {
            int hash = Animator.StringToHash(stateName);
            if (anim.HasState(0, hash))
            {
                if (isInitial)
                {
                    anim.Play(hash, 0, Random.Range(0f, 1f));
                }
                else
                {
                    if (stateInfo.shortNameHash == hash)
                    {
                        if (stateInfo.normalizedTime >= 0.95f && !anim.IsInTransition(0))
                        {
                            anim.Play(hash, 0, 0f);
                        }
                    }
                    else if (!anim.IsInTransition(0))
                    {
                        anim.Play(hash, 0, 0f);
                    }
                }
                stateFound = true;
                break;
            }
        }

        if (!stateFound)
        {
            if (isInitial)
            {
                anim.Play(0, 0, Random.Range(0f, 1f));
            }
            else if (stateInfo.normalizedTime >= 0.95f && !anim.IsInTransition(0))
            {
                anim.Play(0, 0, 0f);
            }
        }
    }

    // Hàm phụ trợ cập nhật hoạt ảnh Train cho danh sách Hologram (đảm bảo lặp liên tục trong suốt thời gian test/nâng cấp)
    private void UpdateHologramAnimations()
    {
        if (spawnedHologramsList == null) return;

        for (int i = 0; i < spawnedHologramsList.Count; i++)
        {
            GameObject hologram = spawnedHologramsList[i];
            if (hologram != null)
            {
                Animator anim = hologram.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    PlayTrainAnimationOnAnimator(anim, false);
                }
            }
        }
    }

    // Hàm lấy số lượng lính tối đa dựa theo Level
    public int GetMaxSoldiersForLevel(int level)
    {
        switch (level)
        {
            case 1: return 4;
            case 2: return 6;
            case 3: return 7;
            default: return 4; // Fallback
        }
    }

    // Hàm lấy sát thương của lính dựa theo Level
    public float GetDamageForLevel(int level)
    {
        switch (level)
        {
            case 1: return 10f;
            case 2: return 20f;
            case 3: return 50f;
            default: return 10f; // Fallback
        }
    }

    // Hàm dùng để spawn một số lượng lính nhất định
    public void SpawnSoldiers(int count)
    {
        if (soldierPrefab == null)
        {
            Debug.LogWarning("Soldier Prefab chưa được gán trong Inspector!");
            return;
        }

        // Tự động dọn dẹp tất cả hologram dư thừa trước khi spawn lính thật
        ClearHolograms();

        // Kiểm tra và đảm bảo không spawn vượt quá số lượng tối đa của Level
        int maxAllowed = GetMaxSoldiersForLevel(currentLevel);
        int activeCount = GetActiveSoldiersCount();

        if (activeCount + count > maxAllowed)
        {
            ClearSpawnedSoldiers();
            count = Mathf.Min(count, maxAllowed);
        }

        float damage = GetDamageForLevel(currentLevel);
        Debug.Log($"[SpawnSoldier] {gameObject.name} (Lv {currentLevel}) đang spawn {count} lính mới với sát thương {damage}.");

        for (int i = 0; i < count; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPosition = new Vector3(
                transform.position.x + randomCircle.x,
                transform.position.y,
                transform.position.z + randomCircle.y
            );

            GameObject soldier = Instantiate(soldierPrefab, spawnPosition, Quaternion.identity);

            UnitController unit = soldier.GetComponent<UnitController>();
            if (unit != null)
            {
                unit.SetAttackDamage(damage);
            }

            // Đảm bảo lính thực TẮT IsTrain và quay về hoạt ảnh Idle ban đầu
            Animator anim = soldier.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                PlayIdleAnimationOnAnimator(anim);
            }

            spawnedSoldiers.Add(soldier);
        }
    }

    // Hàm dọn dẹp các lính cũ đang hoạt động
    private void ClearSpawnedSoldiers()
    {
        Debug.Log($"[SpawnSoldier] ClearSpawnedSoldiers được gọi trên {gameObject.name}. Danh sách đang có {spawnedSoldiers.Count} lính.");

        foreach (GameObject soldier in spawnedSoldiers)
        {
            if (soldier != null)
            {
                Debug.Log($"[SpawnSoldier] Hủy lính trong danh sách: {soldier.name}");
                Destroy(soldier);
            }
        }
        spawnedSoldiers.Clear();

        Collider[] colliders = Physics.OverlapSphere(transform.position, spawnRadius * 3f);
        foreach (var col in colliders)
        {
            if (col != null)
            {
                UnitController unit = col.GetComponentInParent<UnitController>();
                if (unit != null && unit.gameObject != gameObject)
                {
                    Debug.Log($"[SpawnSoldier] Phát hiện và hủy lính lọt lưới xung quanh: {unit.gameObject.name}");
                    Destroy(unit.gameObject);
                }
            }
        }
    }

    // Hàm tạo material hologram mặc định bằng code
    private Material CreateDefaultHologramMaterial()
    {
        if (dynamicHologramMaterial != null)
        {
            return dynamicHologramMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material mat = new Material(shader);
        mat.name = "HologramMaterial_Runtime";

        if (shader.name.Contains("Universal Render Pipeline"))
        {
            mat.SetFloat("_Surface", 1f); // Transparent
            mat.SetFloat("_Blend", 0f); // Alpha Blend
            mat.SetColor("_BaseColor", hologramColor);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else if (shader.name.Contains("Standard"))
        {
            mat.SetFloat("_Mode", 3f); // Transparent
            mat.SetColor("_Color", hologramColor);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else
        {
            mat.color = hologramColor;
        }

        if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", hologramColor);
        }
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", hologramColor);
        }

        dynamicHologramMaterial = mat;
        return dynamicHologramMaterial;
    }

    // Hàm sinh lính hologram để chạy hiệu ứng huấn luyện
    public void SpawnHologramSoldiers(int count)
    {
        if (soldierPrefab == null)
        {
            Debug.LogWarning("Soldier Prefab chưa được gán trong Inspector!");
            return;
        }

        // Đảm bảo xóa hologram cũ nếu có
        ClearHolograms();

        Debug.Log($"[SpawnSoldier] {gameObject.name} (Lv {currentLevel}) đang sinh {count} lính hologram để huấn luyện.");

        Material holoMat = hologramMaterial;
        if (holoMat == null)
        {
            holoMat = CreateDefaultHologramMaterial();
        }

        for (int i = 0; i < count; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPosition = new Vector3(
                transform.position.x + randomCircle.x,
                transform.position.y,
                transform.position.z + randomCircle.y
            );

            GameObject hologram = Instantiate(soldierPrefab, spawnPosition, Quaternion.identity);
            hologram.name = $"{soldierPrefab.name}_Hologram_{i}";

            UnitController unit = hologram.GetComponent<UnitController>();
            if (unit != null)
            {
                unit.enabled = false;
            }

            UnityEngine.AI.NavMeshAgent agent = hologram.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = false;
            }

            Collider[] colliders = hologram.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders)
            {
                if (col != null)
                {
                    col.enabled = false;
                }
            }

            Rigidbody rb = hologram.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            Renderer[] renderers = hologram.GetComponentsInChildren<Renderer>(true);
            foreach (var ren in renderers)
            {
                if (ren != null && holoMat != null)
                {
                    Material[] sharedMats = ren.sharedMaterials;
                    Material[] newMats = new Material[sharedMats.Length];
                    for (int j = 0; j < newMats.Length; j++)
                    {
                        newMats[j] = holoMat;
                    }
                    ren.materials = newMats;
                }
            }

            Animator anim = hologram.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.enabled = true;
                PlayTrainAnimationOnAnimator(anim, true);
            }

            spawnedHologramsList.Add(hologram);
        }

        spawnedHolograms = true;
    }

    // Hàm xóa lính hologram khi hoàn tất nâng cấp
    private void ClearHolograms()
    {
        StopHologramAnimationCoroutine();
        if (spawnedHologramsList != null && spawnedHologramsList.Count > 0)
        {
            Debug.Log($"[SpawnSoldier] ClearHolograms được gọi trên {gameObject.name}. Đang dọn dẹp {spawnedHologramsList.Count} lính hologram.");
            foreach (GameObject hologram in spawnedHologramsList)
            {
                if (hologram != null)
                {
                    Destroy(hologram);
                }
            }
            spawnedHologramsList.Clear();
        }
        spawnedHolograms = false;
    }

    void OnDestroy()
    {
        if (dynamicHologramMaterial != null)
        {
            Destroy(dynamicHologramMaterial);
        }
    }

    [ContextMenu("Upgrade")]
    public void Upgrade()
    {
        if (currentLevel >= 3)
        {
            Debug.Log("Đã đạt cấp độ tối đa (Level 3)!");
            return;
        }

        ClearSpawnedSoldiers();
        currentLevel++;
        int newMax = GetMaxSoldiersForLevel(currentLevel);
        SpawnSoldiers(newMax);

        Debug.Log($"Nâng cấp thành công lên Level {currentLevel}! Đã xóa lính cũ và spawn {newMax} lính mới với sát thương {GetDamageForLevel(currentLevel)}.");
    }

    public int CurrentLevel => currentLevel;

    public int GetActiveSoldiersCount()
    {
        if (spawnedSoldiers == null) return 0;
        int count = 0;
        foreach (GameObject soldier in spawnedSoldiers)
        {
            if (soldier != null)
            {
                count++;
            }
        }
        return count;
    }

    public void LoadAndSpawnSoldiers(int count, int buildingLevel)
    {
        ClearSpawnedSoldiers();
        currentLevel = buildingLevel + 1;
        SpawnSoldiers(count);
    }

    [ContextMenu("Test Training")]
    public void TestTraining5s()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[SpawnSoldier] Chỉ có thể chạy test ở chế độ Play Mode!");
            return;
        }
        StartCoroutine(TestTrainingRoutine());
    }

    private System.Collections.IEnumerator TestTrainingRoutine()
    {
        isTesting = true;
        Debug.Log($"[SpawnSoldier] Bắt đầu chạy thử nghiệm hoạt ảnh Train trong {testDuration} giây...");
        
        // 1. Dọn dẹp lính thực và hologram cũ
        ClearSpawnedSoldiers();
        ClearHolograms();
        
        // 2. Sinh lính hologram tương ứng với level hiện tại của công trình
        int count = GetMaxSoldiersForLevel(currentLevel);
        SpawnHologramSoldiers(count);
        
        spawnedHolograms = true;

        // 3. Đợi trong thời gian testDuration và duy trì lặp lại hoạt ảnh Train cho hologram
        float timer = 0f;
        while (timer < testDuration)
        {
            timer += Time.deltaTime;
            UpdateHologramAnimations();
            yield return null;
        }
        
        // 4. Dọn dẹp hologram khi hoàn tất test
        ClearHolograms();
        
        // 5. Sinh lại lính thực theo level hiện tại và chuyển về Idle
        int countReal = GetMaxSoldiersForLevel(currentLevel);
        SpawnSoldiers(countReal);
        
        isTesting = false;
        Debug.Log($"[SpawnSoldier] Kết thúc chạy thử nghiệm hoạt ảnh Train ({testDuration}s).");
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SpawnSoldier))]
public class SpawnSoldierEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SpawnSoldier spawner = (SpawnSoldier)target;

        GUILayout.Space(15);
        
        GUI.backgroundColor = new Color(0.2f, 0.6f, 1f, 1f);
        
        GUI.enabled = Application.isPlaying;
        if (GUILayout.Button($"Test Training Animation ({spawner.TestDuration}s)", GUILayout.Height(35)))
        {
            spawner.TestTraining5s();
        }
        GUI.enabled = true;
        
        GUI.backgroundColor = Color.white;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Vui lòng vào Play Mode để sử dụng nút Test hoạt ảnh!", MessageType.Info);
        }
    }
}
#endif
