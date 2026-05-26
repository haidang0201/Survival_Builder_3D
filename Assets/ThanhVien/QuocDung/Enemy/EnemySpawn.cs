using UnityEngine;

public class EnemySpawn : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int groupCount = 5;
    [SerializeField] private bool spawnOnStart = true;

    [Header("Group Formation")]
    [SerializeField] private float formationSpacing = 1.2f;
    [SerializeField] private float formationRadius = 2.5f;

    [Header("Spawn Settings")]
    [SerializeField] private float fallbackSpawnRadius = 10f;
    [SerializeField] private float spawnHeightOffset = 1f;
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField] private LayerMask buildingLayer = 0;

    [Header("Manual Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Attack Target")]
    [SerializeField] private Transform attackTarget;

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnGroup();
        }
    }

    public void SpawnGroup()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawn: enemyPrefab is not assigned.", this);
            return;
        }

        if (buildingLayer.value == 0)
        {
            buildingLayer = LayerMask.GetMask("Building");
        }

        Vector3 groupCenter = SampleRandomSetupPoint();
        int columns = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(groupCount)));
        int rows = Mathf.CeilToInt(groupCount / (float)columns);
        EnemyAI leaderAI = null;

        for (int i = 0; i < groupCount; i++)
        {
            Vector3 spawnPosition = GetFormationSpawnPoint(groupCenter, i, columns, rows);
            GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.buildingTarget = attackTarget;

                if (i == 0 || leaderAI == null)
                {
                    enemyAI.isLeader = true;
                    enemyAI.squadLeader = null;
                    leaderAI = enemyAI;
                }
                else
                {
                    enemyAI.isLeader = false;
                    enemyAI.squadLeader = leaderAI.transform;
                }
            }
        }
    }

    private Vector3 GetFormationSpawnPoint(Vector3 groupCenter, int index, int columns, int rows)
    {
        int row = index / columns;
        int column = index % columns;

        float offsetX = (column - (columns - 1) * 0.5f) * formationSpacing;
        float offsetZ = (row - (rows - 1) * 0.5f) * formationSpacing;

        Vector3 offset = new Vector3(offsetX, 0f, offsetZ);
        if (offset.magnitude > formationRadius)
        {
            offset = offset.normalized * formationRadius;
        }

        Vector3 candidate = groupCenter + offset;
        if (TryProjectToGround(candidate, out Vector3 spawnPoint))
        {
            return spawnPoint;
        }

        return groupCenter;
    }

    private Vector3 SampleRandomGroundPoint()
    {
        Vector3 center = transform.position;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * fallbackSpawnRadius;
            Vector3 samplePoint = new Vector3(
                center.x + randomCircle.x,
                center.y + 50f,
                center.z + randomCircle.y
            );

            if (Physics.Raycast(samplePoint, Vector3.down, out RaycastHit hit, 200f, groundLayer))
            {
                samplePoint.y = hit.point.y + spawnHeightOffset;
            }
            else
            {
                continue;
            }

            if (IsBlockedByBuilding(samplePoint))
            {
                continue;
            }

            return samplePoint;
        }

        if (TryProjectToGround(center, out Vector3 groundCenter))
        {
            return groundCenter;
        }

        return center + Vector3.up * spawnHeightOffset;
    }

    private Vector3 SampleRandomSetupPoint()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int randomIndex = Random.Range(0, spawnPoints.Length);
            Transform spawnPoint = spawnPoints[randomIndex];

            if (spawnPoint != null)
            {
                Vector3 point = spawnPoint.position + Vector3.up * 50f;
                if (Physics.Raycast(point, Vector3.down, out RaycastHit hit, 200f, groundLayer))
                {
                    Vector3 groundPoint = hit.point + Vector3.up * spawnHeightOffset;
                    if (!IsBlockedByBuilding(groundPoint))
                    {
                        return groundPoint;
                    }
                }

                if (!IsBlockedByBuilding(spawnPoint.position))
                {
                    return spawnPoint.position + Vector3.up * spawnHeightOffset;
                }
            }
        }

        return SampleRandomGroundPoint();
    }

    private bool TryProjectToGround(Vector3 candidate, out Vector3 result)
    {
        result = candidate;

        Vector3 rayStart = candidate + Vector3.up * 50f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 200f, groundLayer))
        {
            result = hit.point + Vector3.up * spawnHeightOffset;
        }
        else
        {
            return false;
        }

        if (IsBlockedByBuilding(result))
        {
            return false;
        }

        return true;
    }

    private bool IsBlockedByBuilding(Vector3 position)
    {
        if (buildingLayer.value == 0)
        {
            return false;
        }

        return Physics.CheckSphere(position + Vector3.up * 0.5f, 0.5f, buildingLayer, QueryTriggerInteraction.Ignore);
    }
}
