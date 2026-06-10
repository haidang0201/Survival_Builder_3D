using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(WorkerStamina))]
public class WorkerCarrier : MonoBehaviour
{
    public enum CarrierRole  { Universal, WoodOnly, RiceOnly, StoneOnly }
    public enum ResourceType { None, Wood, Rice, Stone }

    [Header("Role Configuration")]
    public CarrierRole role = CarrierRole.Universal;

    [Header("References")]
    public NavMeshAgent agent;
    public Transform    handPoint;
    public Transform    warehousePoint;

    [Header("Resource Pools")]
    public ObjectPool woodPool;
    public ObjectPool ricePool;
    public ObjectPool stonePool;

    [Header("Storage Points (Optional)")]
    public Transform woodStoragePoint;
    public Transform riceStoragePoint;
    public Transform stoneStoragePoint;

    [Header("Animation Settings")]
    public Animator animator;
    public string   speedParam    = "Speed";
    public string   carryingParam = "IsCarrying";

    [Header("Carrier Settings")]
    public float arriveDistance   = 1.5f;
    public float wanderRadius     = 8f;
    public float wanderInterval   = 3f;
    public float checkInterval    = 0.5f;
    public int   maxCarryCapacity = 10;
    public float stuckTimeout     = 2f;

    private WorkerStamina    stamina;
    private WoodStorage      woodStorage;
    private RiceStorage      riceStorage;
    private StoneStorage     stoneStorage;
    private WarehouseStorage warehouseStorage;

    private GameObject   currentVisualObject;
    private bool         isCarrying    = false;
    private int          carriedAmount = 0;
    private ResourceType carriedType   = ResourceType.None;

    private float   wanderTimer    = 0f;
    private float   checkTimer     = 0f;
    private Vector3 anchorPosition;
    private float   stuckTimer     = 0f;

    private enum State { Wander, MoveToStorage, MoveToWarehouse }
    private State currentState = State.Wander;

    private Transform    targetStoragePoint;
    private ResourceType targetResourceType = ResourceType.None;

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        stamina = GetComponent<WorkerStamina>() ?? GetComponentInChildren<WorkerStamina>() ?? GetComponentInParent<WorkerStamina>();
        
        FindReferences();
        anchorPosition = transform.position;
        EnterWander();
    }

    void Update()
    {
        UpdateAnimation();

        bool isNight = DayNightManager.Ins != null && DayNightManager.Ins.CurrentMode == DayNightManager.Mode.Night;

        if (isNight && !isCarrying)
        {
            return; 
        }

        if (stamina != null && !stamina.CanWork())
        {
            if (currentState == State.MoveToWarehouse && isCarrying)
            {
                if (agent != null && agent.isOnNavMesh && agent.isStopped && warehousePoint != null)
                {
                    agent.isStopped = false;
                    agent.SetDestination(warehousePoint.position);
                }
                CheckStuck();
            }
            else
            {
                if (!stamina.IsResting)
                {
                    if (agent != null && agent.isOnNavMesh && !agent.isStopped)
                    {
                        agent.isStopped = true;
                        agent.ResetPath();
                    }
                }
                return;
            }
        }
        else
        {
            CheckStuck();
        }

        switch (currentState)
        {
            case State.Wander:          HandleWander();          break;
            case State.MoveToStorage:   HandleMoveToStorage();   break;
            case State.MoveToWarehouse: HandleMoveToWarehouse(); break;
        }
    }

    void OnDisable()
    {
        if (isCarrying && carriedType != ResourceType.None)
            ReturnResourcesToStorage();
    }

    void UpdateAnimation()
    {
        if (animator == null || agent == null) return;
        float speed = agent.isStopped ? 0f : (agent.speed > 0f ? agent.velocity.magnitude / agent.speed : 0f);
        animator.SetFloat(speedParam, speed, 0.05f, Time.deltaTime);
        animator.SetBool(carryingParam, isCarrying);
    }

    void EnterWander()
    {
        currentState       = State.Wander;
        wanderTimer        = wanderInterval;
        targetStoragePoint = null;
        targetResourceType = ResourceType.None;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        
        // RẢNH RỖI THÌ TẮT TIÊU HAO THỂ LỰC
        stamina?.SetDraining(false); 
    }

    void HandleWander()
    {
        wanderTimer += Time.deltaTime;
        if (wanderTimer >= wanderInterval)
        {
            wanderTimer = 0f;
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
            {
                if (agent.isOnNavMesh)
                    agent.SetDestination(GetRandomWanderPoint());
            }
        }

        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;
            if (TrySelectStorageToClear())
            {
                EnterMoveToStorage();
                return;
            }
        }
    }

    bool TrySelectStorageToClear()
    {
        int          maxAmount = 0;
        ResourceType bestType  = ResourceType.None;
        Transform    bestPoint = null;

        if ((role == CarrierRole.Universal || role == CarrierRole.WoodOnly)
            && woodStorage != null && !woodStorage.IsEmpty && woodStorage.CurrentAmount > maxAmount)
        {
            maxAmount = woodStorage.CurrentAmount;
            bestType  = ResourceType.Wood;
            bestPoint = woodStoragePoint;
        }

        if ((role == CarrierRole.Universal || role == CarrierRole.RiceOnly)
            && riceStorage != null && !riceStorage.IsEmpty && riceStorage.CurrentAmount > maxAmount)
        {
            maxAmount = riceStorage.CurrentAmount;
            bestType  = ResourceType.Rice;
            bestPoint = riceStoragePoint;
        }

        if ((role == CarrierRole.Universal || role == CarrierRole.StoneOnly)
            && stoneStorage != null && !stoneStorage.IsEmpty && stoneStorage.CurrentAmount > maxAmount)
        {
            maxAmount = stoneStorage.CurrentAmount;
            bestType  = ResourceType.Stone;
            bestPoint = stoneStoragePoint;
        }

        if (bestType == ResourceType.None) return false;

        targetResourceType = bestType;
        targetStoragePoint = bestPoint;
        return true;
    }

    void EnterMoveToStorage()
    {
        if (targetStoragePoint == null || !agent.isOnNavMesh)
        {
            EnterWander();
            return;
        }
        currentState    = State.MoveToStorage;
        agent.isStopped = false;
        agent.SetDestination(targetStoragePoint.position);

        // BẬT TIÊU HAO THỂ LỰC KHI ĐI LÀM
        stamina?.SetDraining(true); 
    }

    void HandleMoveToStorage()
    {
        if (IsTargetStorageEmpty())
        {
            if (TrySelectStorageToClear()) EnterMoveToStorage();
            else EnterWander();
            return;
        }

        if (!HasArrived()) return;

        agent.isStopped = true;
        int taken = 0;

        switch (targetResourceType)
        {
            case ResourceType.Wood:  if (woodStorage  != null) taken = woodStorage.TakeWood(maxCarryCapacity);   break;
            case ResourceType.Rice:  if (riceStorage  != null) taken = riceStorage.TakeRice(maxCarryCapacity);   break;
            case ResourceType.Stone: if (stoneStorage != null) taken = stoneStorage.TakeStone(maxCarryCapacity); break;
        }

        if (taken <= 0)
        {
            if (TrySelectStorageToClear()) EnterMoveToStorage();
            else EnterWander();
            return;
        }

        carriedAmount = taken;
        carriedType   = targetResourceType;
        isCarrying    = true;

        if (stamina != null) stamina.isCarryingResources = true;

        SpawnCarriedVisual();
        EnterMoveToWarehouse();
    }

    void EnterMoveToWarehouse()
    {
        if (warehousePoint == null || !agent.isOnNavMesh)
        {
            ReturnResourcesToStorage();
            EnterWander();
            return;
        }
        currentState    = State.MoveToWarehouse;
        agent.isStopped = false;
        agent.SetDestination(warehousePoint.position);

        // BẬT TIÊU HAO THỂ LỰC KHI ĐI LÀM
        stamina?.SetDraining(true); 
    }

    void HandleMoveToWarehouse()
    {
        if (!isCarrying)
        {
            if (TrySelectStorageToClear()) EnterMoveToStorage();
            else EnterWander();
            return;
        }

        if (!HasArrived()) return;

        agent.isStopped = true;

        if (warehouseStorage != null)
        {
            switch (carriedType)
            {
                case ResourceType.Wood:  warehouseStorage.AddWood(carriedAmount);  break;
                case ResourceType.Rice:  warehouseStorage.AddRice(carriedAmount);  break;
                case ResourceType.Stone: warehouseStorage.AddStone(carriedAmount); break;
            }
        }

        ReturnVisualToPool();
        ResetCarry();

        stamina?.OnResourcesDeposited();

        if (stamina == null || stamina.CanWork())
        {
            if (TrySelectStorageToClear()) EnterMoveToStorage();
            else EnterWander();
        }
    }

    bool HasArrived()
    {
        return !agent.pathPending && agent.remainingDistance <= (agent.stoppingDistance + arriveDistance);
    }

    bool IsTargetStorageEmpty()
    {
        switch (targetResourceType)
        {
            case ResourceType.Wood:  return woodStorage  == null || woodStorage.IsEmpty;
            case ResourceType.Rice:  return riceStorage  == null || riceStorage.IsEmpty;
            case ResourceType.Stone: return stoneStorage == null || stoneStorage.IsEmpty;
            default: return true;
        }
    }

    void CheckStuck()
    {
        if (agent == null || agent.isStopped || !agent.hasPath)
        {
            stuckTimer = 0f;
            return;
        }

        if (agent.velocity.sqrMagnitude < 0.01f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckTimeout)
            {
                stuckTimer = 0f;
                if (currentState == State.Wander)
                {
                    if (agent.isOnNavMesh)
                    {
                        agent.ResetPath();
                        agent.SetDestination(GetRandomWanderPoint());
                    }
                }
                else
                {
                    if (agent.isOnNavMesh)
                        agent.SetDestination(agent.destination);
                }
            }
        }
        else stuckTimer = 0f;
    }

    Vector3 GetRandomWanderPoint()
    {
        Vector3 randDir = Random.insideUnitSphere * wanderRadius + anchorPosition;
        if (NavMesh.SamplePosition(randDir, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            return hit.position;
        return anchorPosition;
    }

    void SpawnCarriedVisual()
    {
        ObjectPool activePool = GetPoolForType(carriedType);
        if (activePool == null) return;

        currentVisualObject = activePool.GetObject();
        if (currentVisualObject == null) return;

        switch (carriedType)
        {
            case ResourceType.Wood:
                var wp = currentVisualObject.GetComponent<WoodPickup>();
                if (wp != null) { wp.MarkTaken(); wp.Pickup(handPoint); }
                break;
            case ResourceType.Rice:
                var rp = currentVisualObject.GetComponent<RicePickup>();
                if (rp != null) { rp.MarkTaken(); rp.Pickup(handPoint); }
                break;
            case ResourceType.Stone:
                var sp = currentVisualObject.GetComponent<StonePickup>();
                if (sp != null) { sp.MarkTaken(); sp.Pickup(handPoint); }
                break;
        }
    }

    void ReturnVisualToPool()
    {
        if (currentVisualObject == null) return;

        ObjectPool activePool = GetPoolForType(carriedType);
        if (activePool != null && currentVisualObject.activeInHierarchy)
            activePool.ReturnObject(currentVisualObject);
        else
            Destroy(currentVisualObject);

        currentVisualObject = null;
    }

    ObjectPool GetPoolForType(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Wood:  return woodPool;
            case ResourceType.Rice:  return ricePool;
            case ResourceType.Stone: return stonePool;
            default: return null;
        }
    }

    void ReturnResourcesToStorage()
    {
        switch (carriedType)
        {
            case ResourceType.Wood:  woodStorage?.AddWood(carriedAmount);   break;
            case ResourceType.Rice:  riceStorage?.AddRice(carriedAmount);   break;
            case ResourceType.Stone: stoneStorage?.AddStone(carriedAmount); break;
        }
        ReturnVisualToPool();
        ResetCarry();
    }

    void ResetCarry()
    {
        isCarrying    = false;
        carriedAmount = 0;
        carriedType   = ResourceType.None;
        if (stamina != null) stamina.isCarryingResources = false;
    }

    void FindReferences()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        if (warehousePoint == null)
        {
            GameObject wh = GameObject.FindWithTag("Warehouse");
            if (wh != null) warehousePoint = wh.transform;
        }
        if (warehousePoint != null)
            warehouseStorage = warehousePoint.GetComponent<WarehouseStorage>() ?? warehousePoint.GetComponentInChildren<WarehouseStorage>() ?? warehousePoint.GetComponentInParent<WarehouseStorage>();

        if (woodStoragePoint == null)
        {
            GameObject obj = GameObject.FindWithTag("Storage");
            if (obj != null) woodStoragePoint = obj.transform;
        }
        if (woodStoragePoint != null)
            woodStorage = woodStoragePoint.GetComponent<WoodStorage>() ?? woodStoragePoint.GetComponentInChildren<WoodStorage>() ?? woodStoragePoint.GetComponentInParent<WoodStorage>();

        if (riceStoragePoint == null)
        {
            GameObject obj = GameObject.FindWithTag("RiceStorage");
            if (obj != null) riceStoragePoint = obj.transform;
        }
        if (riceStoragePoint != null)
            riceStorage = riceStoragePoint.GetComponent<RiceStorage>() ?? riceStoragePoint.GetComponentInChildren<RiceStorage>() ?? riceStoragePoint.GetComponentInParent<RiceStorage>();

        if (stoneStoragePoint == null)
        {
            GameObject obj = GameObject.FindWithTag("StoneStorage");
            if (obj != null) stoneStoragePoint = obj.transform;
        }
        if (stoneStoragePoint != null)
            stoneStorage = stoneStoragePoint.GetComponent<StoneStorage>() ?? stoneStoragePoint.GetComponentInChildren<StoneStorage>() ?? stoneStoragePoint.GetComponentInParent<StoneStorage>();
    }
}