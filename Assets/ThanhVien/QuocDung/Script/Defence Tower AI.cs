using UnityEngine;

public class DefenceTowerAI : MonoBehaviour
{
    [Header("Cấu hình Khiên (Shield)")]
    [Tooltip("Đối tượng Khiên cần dựng lên/hạ xuống (Kéo từ Hierarchy hoặc kéo file Prefab từ Project đều được)")]
    public Transform shieldObject;
    
    [Tooltip("Tốc độ dựng và hạ khiên")]
    public float transitionSpeed = 3f;

    [Tooltip("Khoảng cách hạ khiên xuống dưới đất khi ẩn")]
    public float shieldHeightOffset = 5f;

    [Header("Cấu hình Vị trí Khiên khi Hoạt động (Active)")]
    [Tooltip("Tích chọn để tự động lấy vị trí/xoay ban đầu của khiên trong Scene làm vị trí dựng khiên chuẩn. Bỏ tích nếu muốn tự nhập tọa độ bên dưới.")]
    public bool useInitialTransform = true;

    [Tooltip("Vị trí cục bộ (Local Position) của khiên khi dựng lên hoàn tất (chỉ dùng khi useInitialTransform = false hoặc khi sinh khiên từ Prefab mới)")]
    public Vector3 shieldActiveLocalPos = new Vector3(0f, 0f, 3f);

    [Tooltip("Góc quay cục bộ (Local Rotation) của khiên khi hoạt động (chỉ dùng khi useInitialTransform = false hoặc khi sinh khiên từ Prefab mới)")]
    public Vector3 shieldActiveLocalRot = Vector3.zero;

    [Header("Quét Tìm Kẻ Địch (Enemy)")]
    [Tooltip("Bán kính phát hiện kẻ địch xung quanh tháp")]
    public float detectRadius = 15f;
    
    [Tooltip("LayerMask của Enemy để tối ưu hóa hiệu năng quét. Nếu chọn Nothing, script sẽ tự động dò tìm bằng mọi layer.")]
    public LayerMask enemyLayer;

    [Tooltip("Thời gian giãn cách giữa các lần quét (giây) tránh giật lag")]
    public float scanInterval = 0.2f;

    private float nextScanTime;
    private bool isEnemyNearby;
    private bool isPrefabReference; // Đánh dấu nếu shield được gán từ Prefab ngoài Project
    
    // Lưu trữ vị trí và góc quay cục bộ khi khiên hoạt động bình thường
    private Vector3 activeLocalPos;
    private Vector3 activeLocalRot;
    private Vector3 inactiveLocalPos;

    [Header("Cấu hình Cấp độ 3 (Knockback)")]
    public float knockbackInterval = 3f;
    public float knockbackDistance = 5f;
    public float knockbackDuration = 0.3f;
    public float knockbackRadius = 12f;

    private UpgradeableBuilding upgradeableBuilding;
    private int lastCheckedLevel = -1;
    private float nextKnockbackTime;

    private void Start()
    {
        upgradeableBuilding = GetComponent<UpgradeableBuilding>();

        // 1. Tự động sửa lỗi nếu kéo trực tiếp file Prefab từ Project vào thay vì đối tượng con trong Hierarchy
        HandlePrefabShieldReference();

        if (shieldObject == null)
        {
            Debug.LogError($"[DefenceTowerAI] {name}: Chưa gán shieldObject và không tìm thấy đối tượng khiên con!");
            enabled = false;
            return;
        }

        // Đảm bảo đối tượng shieldObject có component Shield
        if (shieldObject.GetComponent<Shield>() == null)
        {
            shieldObject.gameObject.AddComponent<Shield>();
            Debug.Log($"[DefenceTowerAI] {name}: Đã tự động gắn component Shield vào '{shieldObject.name}'.");
        }

        // 2. Thiết lập vị trí và góc quay hoạt động (Active State)
        // Nếu là Prefab Asset thì bắt buộc dùng các thông số trong Inspector thay vì lấy Initial của Prefab
        if (useInitialTransform && !isPrefabReference)
        {
            activeLocalPos = shieldObject.localPosition;
            activeLocalRot = shieldObject.localEulerAngles;
        }
        else
        {
            activeLocalPos = shieldActiveLocalPos;
            activeLocalRot = shieldActiveLocalRot;

            // Áp dụng ngay vị trí và góc quay cấu hình thủ công cho khiên
            shieldObject.localPosition = activeLocalPos;
            shieldObject.localEulerAngles = activeLocalRot;
        }

        // 3. Tính toán vị trí khi không hoạt động (Inactive)
        inactiveLocalPos = activeLocalPos + Vector3.down * shieldHeightOffset;

        // 4. Khởi tạo trạng thái ban đầu của khiên là ẩn/hạ xuống
        InitializeShieldState();

        int currentLevel = upgradeableBuilding != null ? upgradeableBuilding.CurrentLevel : 0;
        lastCheckedLevel = currentLevel;
        UpdateShieldComponent();
    }

    private void HandlePrefabShieldReference()
    {
        if (shieldObject != null)
        {
            // Kiểm tra xem transform được gán có thuộc về một Scene đang chạy không.
            if (!shieldObject.gameObject.scene.IsValid())
            {
                isPrefabReference = true;
                Debug.LogWarning($"[DefenceTowerAI] {name}: Ô 'Shield Object' đang trỏ tới một Prefab Asset trong Project. Script sẽ tự động xử lý...");

                // Tìm xem tháp đã có sẵn một đối tượng con tên là "Shield" hay chưa
                Transform existingShield = transform.Find("Shield");
                if (existingShield != null)
                {
                    shieldObject = existingShield;
                    isPrefabReference = false; // Đã tìm thấy instance trong scene
                    Debug.Log($"[DefenceTowerAI] {name}: Đã tự động liên kết với thực thể khiên con '{existingShield.name}' có sẵn trong Hierarchy.");
                }
                else
                {
                    // Lưu lại kích thước gốc của Prefab
                    Vector3 originalPrefabScale = shieldObject.localScale;

                    // Nếu chưa có đối tượng con nào, sinh ra (Instantiate) một đối tượng mới làm con của tháp
                    GameObject spawnedShield = Instantiate(shieldObject.gameObject, transform);
                    spawnedShield.name = "Shield";
                    
                    // Thiết lập lại vị trí và góc quay cục bộ của khiên dựa theo cài đặt trong Inspector
                    spawnedShield.transform.localPosition = shieldActiveLocalPos;
                    spawnedShield.transform.localRotation = Quaternion.Euler(shieldActiveLocalRot);
                    
                    // GIỮ NGUYÊN HOÀN TOÀN SCALE GỐC CỦA PREFAB
                    spawnedShield.transform.localScale = originalPrefabScale;
                    
                    shieldObject = spawnedShield.transform;
                    Debug.Log($"[DefenceTowerAI] {name}: Đã tự động sinh ra một thực thể khiên mới từ Prefab làm con của tháp tại vị trí: {shieldActiveLocalPos} (Giữ nguyên Scale: {originalPrefabScale})");
                }
            }
        }
        else
        {
            // Nếu lập trình viên quên kéo khiên vào Inspector, tự động tìm đối tượng con tên là "Shield"
            Transform existingShield = transform.Find("Shield");
            if (existingShield != null)
            {
                shieldObject = existingShield;
                Debug.Log($"[DefenceTowerAI] {name}: Tự động tìm thấy đối tượng khiên con '{existingShield.name}' trong Hierarchy.");
            }
        }
    }

    private void InitializeShieldState()
    {
        shieldObject.localPosition = inactiveLocalPos;
        
        // Mặc định ẩn hẳn GameObject của khiên khi mới bắt đầu để tối ưu hiệu năng
        shieldObject.gameObject.SetActive(false);
    }

    private bool CanOperate()
    {
        if (upgradeableBuilding == null)
        {
            upgradeableBuilding = GetComponent<UpgradeableBuilding>();
            if (upgradeableBuilding == null) upgradeableBuilding = GetComponentInParent<UpgradeableBuilding>();
            if (upgradeableBuilding == null) upgradeableBuilding = GetComponentInChildren<UpgradeableBuilding>();
        }

        if (upgradeableBuilding != null && (upgradeableBuilding.IsInitialBuildNeeded || upgradeableBuilding.IsUpgrading || upgradeableBuilding.IsRuined))
        {
            return false;
        }

        BuildingCtrl ctrl = GetComponent<BuildingCtrl>();
        if (ctrl == null) ctrl = GetComponentInParent<BuildingCtrl>();
        if (ctrl != null && !ctrl.IsBuilt)
        {
            return false;
        }

        return true;
    }

    private void Update()
    {
        if (!CanOperate())
        {
            isEnemyNearby = false;
            UpdateShieldTransition();
            return;
        }
        // Cập nhật thông số thời gian thực khi đang ở trong Unity Editor (hỗ trợ điều chỉnh lúc Play Mode)
#if UNITY_EDITOR
        UpdateParamsInEditor();
#endif

        int currentLevel = upgradeableBuilding != null ? upgradeableBuilding.CurrentLevel : 0;
        if (currentLevel != lastCheckedLevel)
        {
            lastCheckedLevel = currentLevel;
            UpdateShieldComponent();
        }

        // 1. Quét tìm Enemy định kỳ để tránh quá tải CPU
        if (Time.time >= nextScanTime)
        {
            ScanForEnemies();
            nextScanTime = Time.time + scanInterval;
        }

        if (currentLevel == 2 && isEnemyNearby)
        {
            if (Time.time >= nextKnockbackTime)
            {
                PushBackEnemies();
                nextKnockbackTime = Time.time + knockbackInterval;
            }
        }

        // 2. Chuyển đổi trạng thái khiên mượt mà bằng Lerp
        UpdateShieldTransition();
    }

#if UNITY_EDITOR
    private void UpdateParamsInEditor()
    {
        // Nếu không sử dụng vị trí ban đầu trong Scene, hoặc đối tượng khiên được sinh ra từ Prefab
        if (!useInitialTransform || isPrefabReference)
        {
            activeLocalPos = shieldActiveLocalPos;
            activeLocalRot = shieldActiveLocalRot;

            // Nếu khiên đang hoạt động, cập nhật luôn góc quay cục bộ thời gian thực
            if (shieldObject != null && isEnemyNearby)
            {
                shieldObject.localEulerAngles = activeLocalRot;
            }
        }
        
        inactiveLocalPos = activeLocalPos + Vector3.down * shieldHeightOffset;
    }
#endif

    private void ScanForEnemies()
    {
        Collider[] enemies;

        // Tự động sửa lỗi cấu hình: Nếu LayerMask là Nothing (value == 0), quét tất cả các Layer
        if (enemyLayer.value == 0)
        {
            enemies = Physics.OverlapSphere(transform.position, detectRadius);
        }
        else
        {
            enemies = Physics.OverlapSphere(transform.position, detectRadius, enemyLayer);
        }

        bool foundEnemy = false;

        foreach (var col in enemies)
        {
            if (col == null) continue;

            // Nhận diện Enemy bằng nhiều cách an toàn:
            // 1. Kiểm tra Tag là "Enemy"
            // 2. Kiểm tra xem có chứa component EnemyHealth không
            // 3. Tên đối tượng có chứa chữ "enemy"
            if (col.CompareTag("Enemy") || 
                col.GetComponent<EnemyHealth>() != null || 
                col.name.ToLower().Contains("enemy"))
            {
                foundEnemy = true;
                break;
            }
        }

        // In log ra Console khi trạng thái tháp thay đổi để dễ dàng theo dõi
        if (foundEnemy != isEnemyNearby)
        {
            Debug.Log($"[DefenceTowerAI] {name}: Trạng thái đổi -> Có Enemy ở gần = {foundEnemy}");
        }

        isEnemyNearby = foundEnemy;
    }

    private void UpdateShieldTransition()
    {
        // Quyết định vị trí mục tiêu dựa trên việc có Enemy ở gần hay không
        Vector3 targetPos = isEnemyNearby ? activeLocalPos : inactiveLocalPos;

        // Kích hoạt GameObject của khiên nếu có quái hoặc khiên đang trong quá trình hạ xuống chưa xong
        bool shouldBeActive = isEnemyNearby || !IsShieldFullyInactive();
        
        if (shieldObject.gameObject.activeSelf != shouldBeActive)
        {
            shieldObject.gameObject.SetActive(shouldBeActive);
        }

        if (shieldObject.gameObject.activeSelf)
        {
            // Thực hiện chuyển đổi mượt mà bằng Lerp vị trí
            shieldObject.localPosition = Vector3.Lerp(shieldObject.localPosition, targetPos, Time.deltaTime * transitionSpeed);

            // Khi quái đã đi xa và khiên đã thu về vị trí ẩn hoàn toàn -> Tắt hẳn GameObject để tối ưu render
            if (!isEnemyNearby && IsShieldFullyInactive())
            {
                shieldObject.gameObject.SetActive(false);
            }
        }
    }

    // Kiểm tra xem khiên đã thu về trạng thái ẩn hoàn toàn hay chưa (sử dụng sai số nhỏ)
    private bool IsShieldFullyInactive()
    {
        if (shieldObject == null) return true;
        return Vector3.Distance(shieldObject.localPosition, inactiveLocalPos) < 0.05f;
    }

    // Hàm cho phép các Script/Tháp khác kích hoạt hoặc tắt khiên trực tiếp
    public void ForceTriggerShield(bool activate)
    {
        isEnemyNearby = activate;
        // Trì hoãn lượt quét tự động tiếp theo để giữ trạng thái được điều khiển
        nextScanTime = Time.time + scanInterval * 2f;
    }

    // Vẽ vòng bán kính quét quái màu xanh dương trong Unity Editor để dễ thiết lập bán kính
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }

    private void UpdateShieldComponent()
    {
        int level = upgradeableBuilding != null ? upgradeableBuilding.CurrentLevel : 0;
        
        Transform activeShield = null;
        if (upgradeableBuilding != null && upgradeableBuilding.VisualModels != null && level < upgradeableBuilding.VisualModels.Length)
        {
            GameObject activeModel = upgradeableBuilding.VisualModels[level];
            if (activeModel != null)
            {
                Shield sComp = activeModel.GetComponentInChildren<Shield>(true);
                if (sComp != null)
                {
                    activeShield = sComp.transform;
                }
                else
                {
                    Transform tShield = activeModel.transform.Find("Shield");
                    if (tShield != null) activeShield = tShield;
                }
            }
        }

        if (activeShield == null)
        {
            activeShield = shieldObject;
        }

        if (activeShield != null)
        {
            shieldObject = activeShield;

            Shield shieldComp = activeShield.GetComponent<Shield>();
            if (shieldComp == null)
            {
                shieldComp = activeShield.gameObject.AddComponent<Shield>();
            }

            shieldComp.Level = level + 1;
            if (level == 0)
            {
                shieldComp.damageReductionPercent = 0.2f;
                shieldComp.blockChance = 0f;
            }
            else if (level == 1)
            {
                shieldComp.damageReductionPercent = 0.4f;
                shieldComp.blockChance = 0.5f; // 50% block chance
            }
            else if (level == 2)
            {
                shieldComp.damageReductionPercent = 0.6f;
                shieldComp.blockChance = 0f;
            }

            Debug.Log($"[DefenceTowerAI] {name}: Cập nhật cấu hình khiên '{activeShield.name}' cho Cấp {level + 1}: giảm {shieldComp.damageReductionPercent * 100}%, né {shieldComp.blockChance * 100}%");
        }
    }

    private void PushBackEnemies()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, knockbackRadius);
        foreach (var c in colliders)
        {
            if (c == null) continue;
            if (c.CompareTag("Enemy") || c.GetComponentInParent<EnemyHealth>() != null)
            {
                var enemyAI = c.GetComponentInParent<EnemyAI>();
                if (enemyAI != null)
                {
                    Vector3 pushDir = (enemyAI.transform.position - transform.position);
                    pushDir.y = 0f;
                    if (pushDir.sqrMagnitude < 0.0001f) pushDir = Vector3.forward;
                    pushDir.Normalize();
                    
                    enemyAI.Knockback(pushDir, knockbackDistance, knockbackDuration);
                    Debug.Log($"[DefenceTowerAI] {name}: Đẩy lùi kẻ địch {enemyAI.name}");
                }
            }
        }
    }
}
