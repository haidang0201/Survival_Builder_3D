using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Script độc lập: khi worker phát hiện Enemy (qua Tag) trong bán kính detectRadius,
/// worker sẽ NGAY LẬP TỨC bỏ việc đang làm và chạy về nhà (House) trú ẩn, bất kể đang ngày hay đêm.
/// 
/// Thiết kế để KHÔNG xung đột với WorkerFindTree / WorkerFindRice / WorkerFindStone / WorkerStamina:
/// - Không sửa bất kỳ dòng nào trong các script đó.
/// - Khi phát hiện enemy: tạm thời DISABLE 3 script Find* (nếu có gắn trên object) 
///   để chúng ngưng điều khiển agent/animator, sau đó script này tự lái agent chạy về nhà,
///   TỰ cập nhật animation Speed (vì Find* đang bị tắt, không còn ai làm việc đó),
///   và khi tới nơi thì gọi House.Enter() thật sự để được ẩn model như lúc resting bình thường.
/// - Khi hết nguy hiểm: gọi House.Exit(), hiện lại model, rồi ENABLE lại các script Find*, 
///   chúng tiếp tục logic bình thường từ đúng state cũ (Update() của chúng chỉ đơn giản là
///   không chạy trong lúc bị disable, không hề bị reset field nào).
/// - Không đụng vào WorkerStamina, chỉ tạm dừng "drain" bằng SetDraining(false) để tránh 
///   bị tính hao thể lực do "đang làm việc" trong lúc chạy trốn.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class WorkerEnemyFlee : MonoBehaviour
{
    [Header("Detect Settings")]
    public string enemyTag = "Enemy";
    public float  detectRadius   = 8f;
    public float  detectInterval = 0.25f; // quét enemy mỗi X giây, đỡ tốn hiệu năng
    public float  safeExtraRadius = 2f;   // phải xa hơn detectRadius bao nhiêu mới coi là "an toàn" (tránh giật lật liên tục)
    public float  arriveDistance = 2.0f;  // khoảng cách tới cửa nhà được coi là "đã tới" để vào trú

    [Header("Home / Fallback")]
    public House   house;
    public Transform fallbackHomeSpot; // nếu không tìm được House, chạy về đây (sẽ không Enter/hide, chỉ đứng chờ)

    [Header("Model to hide when hiding inside house")]
    [Tooltip("Nếu để trống, script sẽ tự lấy workerModel/extraModelsToHide từ WorkerStamina trên cùng object (nếu có).")]
    public GameObject workerModel;
    public GameObject[] extraModelsToHide;

    [Header("Animation")]
    public Animator animator;
    public string   speedParamName = "Speed"; // trùng tên với các script Find* để không phá animator controller
    public string   fleeBoolName = "";        // để trống nếu không cần bool riêng cho trạng thái hoảng loạn

    private NavMeshAgent agent;
    private WorkerStamina stamina;

    private MonoBehaviour[] findScriptsToDisable;
    private bool[] wasEnabledBeforeFlee;

    private bool isFleeing = false;
    private bool isTakingShelter = false; // đã vào bên trong nhà (Enter thành công), đứng yên & ẩn model
    private float detectTimer = 0f;
    private Vector3 homePosition;
    private bool hasHomePosition = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        stamina = GetComponent<WorkerStamina>();
        if (animator == null) animator = GetComponent<Animator>();

        // Nếu chưa gán model trong Inspector, thử lấy lại từ WorkerStamina để dùng chung
        // (WorkerStamina đã có sẵn field workerModel/extraModelsToHide public).
        if (workerModel == null && stamina != null) workerModel = stamina.workerModel;
        if ((extraModelsToHide == null || extraModelsToHide.Length == 0) && stamina != null)
            extraModelsToHide = stamina.extraModelsToHide;

        // Gom các script "đi làm việc" hiện có trên object, để bật/tắt khi cần,
        // không cần biết trước worker này là loại nào (rice/tree/stone).
        var list = new System.Collections.Generic.List<MonoBehaviour>();
        var findTree  = GetComponent<WorkerFindTree>();
        var findRice  = GetComponent<WorkerFindRice>();
        var findStone = GetComponent<WorkerFindStone>();
        if (findTree  != null) list.Add(findTree);
        if (findRice  != null) list.Add(findRice);
        if (findStone != null) list.Add(findStone);
        findScriptsToDisable = list.ToArray();
        wasEnabledBeforeFlee = new bool[findScriptsToDisable.Length];
    }

    void Update()
    {
        detectTimer -= Time.deltaTime;
        if (detectTimer <= 0f)
        {
            detectTimer = detectInterval;
            CheckEnemyPresence();
        }

        if (isFleeing)
        {
            HandleFleeMovement();
            UpdateFleeAnimationSpeed();
        }
    }

    void CheckEnemyPresence()
    {
        bool enemyNearby = IsEnemyWithin(detectRadius);

        if (!isFleeing && enemyNearby)
        {
            StartFleeing();
        }
        else if (isFleeing && !enemyNearby)
        {
            // dùng bán kính an toàn lớn hơn để tránh bật/tắt liên tục ở biên detectRadius
            bool stillSafe = !IsEnemyWithin(detectRadius + safeExtraRadius);
            if (stillSafe) StopFleeing();
        }
    }

    bool IsEnemyWithin(float radius)
    {
        // Dùng OverlapSphere theo tag, không cần Registry riêng, không đụng code khác.
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].CompareTag(enemyTag)) return true;
        }
        return false;
    }

    void StartFleeing()
    {
        isFleeing = true;
        isTakingShelter = false;

        // Tắt các script "đi làm" để chúng không tranh giành điều khiển agent/animator.
        for (int i = 0; i < findScriptsToDisable.Length; i++)
        {
            wasEnabledBeforeFlee[i] = findScriptsToDisable[i].enabled;
            findScriptsToDisable[i].enabled = false;
        }

        // Không ép resting, chỉ tắt drain để không bị tính hao stamina do "đang làm việc"
        stamina?.SetDraining(false);

        ResolveHomePosition();

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            if (hasHomePosition) agent.SetDestination(homePosition);
        }

        if (!string.IsNullOrEmpty(fleeBoolName) && animator != null)
            animator.SetBool(fleeBoolName, true);
    }

    void ResolveHomePosition()
    {
        if (house != null)
        {
            homePosition = house.EntrancePosition;
            hasHomePosition = true;
            return;
        }

        if (fallbackHomeSpot != null)
        {
            homePosition = fallbackHomeSpot.position;
            hasHomePosition = true;
            return;
        }

        // thử tìm House trong scene như một phương án cuối
        House found = FindObjectOfType<House>();
        if (found != null)
        {
            house = found;
            homePosition = found.EntrancePosition;
            hasHomePosition = true;
        }
        else
        {
            hasHomePosition = false;
        }
    }

    void HandleFleeMovement()
    {
        if (isTakingShelter) return; // đã vào nhà, đứng yên & ẩn model, không cần di chuyển nữa

        if (agent == null || !agent.isOnNavMesh || !hasHomePosition) return;

        // Nếu House đổi trạng thái hoặc bị null giữa chừng, thử lại
        if (agent.destination != homePosition && !agent.pathPending)
        {
            agent.SetDestination(homePosition);
        }

        bool arrived = !agent.pathPending && agent.remainingDistance <= Mathf.Max(arriveDistance, agent.stoppingDistance + 0.1f);
        if (!arrived) return;

        // Đã tới cửa nhà: thử vào trú thật sự (giống cơ chế resting của WorkerStamina)
        if (house != null && house.Enter(stamina != null ? stamina : GetComponent<WorkerStamina>()))
        {
            isTakingShelter = true;
            agent.isStopped = true;
            HideModel();
        }
        // Nếu house null (đang dùng fallbackHomeSpot) hoặc House đầy chỗ,
        // worker vẫn đứng đó chờ, KHÔNG hide model — an toàn hơn là đứng giữa đường chờ enemy đi qua.
    }

    void UpdateFleeAnimationSpeed()
    {
        if (isTakingShelter || animator == null || agent == null) return;
        float speed = agent.isStopped ? 0f : (agent.speed > 0f ? agent.velocity.magnitude / agent.speed : 0f);
        animator.SetFloat(speedParamName, speed, 0.05f, Time.deltaTime);
    }

    void StopFleeing()
    {
        isFleeing = false;

        // Nếu đang trú trong nhà, phải Exit() và hiện lại model trước khi trả quyền lại cho Find*
        if (isTakingShelter)
        {
            house?.Exit(stamina != null ? stamina : GetComponent<WorkerStamina>());
            ShowModel();
            isTakingShelter = false;
            if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        }

        for (int i = 0; i < findScriptsToDisable.Length; i++)
        {
            findScriptsToDisable[i].enabled = wasEnabledBeforeFlee[i];
        }

        if (!string.IsNullOrEmpty(fleeBoolName) && animator != null)
            animator.SetBool(fleeBoolName, false);

        // Không cần reset agent thủ công: script Find* tương ứng sẽ tự
        // SetDestination lại theo state cũ của nó ngay ở frame kế tiếp.
    }

    void HideModel()
    {
        if (workerModel != null) workerModel.SetActive(false);
        if (extraModelsToHide != null)
            foreach (var obj in extraModelsToHide) if (obj != null) obj.SetActive(false);
    }

    void ShowModel()
    {
        if (workerModel != null) workerModel.SetActive(true);
        if (extraModelsToHide != null)
            foreach (var obj in extraModelsToHide) if (obj != null) obj.SetActive(true);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = isFleeing ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}