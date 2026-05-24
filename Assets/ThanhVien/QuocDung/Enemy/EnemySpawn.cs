using UnityEngine;
using UnityEngine.AI;

public class EnemySpawn : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int groupCount = 5;
    [SerializeField] private bool spawnOnStart = true;

    [Header("Terrain")]
    [SerializeField] private Terrain spawnTerrain;
    [SerializeField] private int formationColumns = 3;
    [SerializeField] private float spacing = 1.5f;
    [SerializeField] private float navMeshSampleRadius = 2f;
    [SerializeField] private float spawnHeightOffset = 1f;

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

        Terrain terrain = spawnTerrain != null ? spawnTerrain : Terrain.activeTerrain;
        if (terrain == null)
        {
            Debug.LogError("EnemySpawn: no Terrain found. Assign spawnTerrain or keep an active terrain in the scene.", this);
            return;
        }

        Vector3 groupCenter = GetRandomTerrainPoint(terrain);
        int columns = Mathf.Max(1, formationColumns);
        int rows = Mathf.CeilToInt(groupCount / (float)columns);

        for (int i = 0; i < groupCount; i++)
        {
            Vector3 spawnPosition = GetRectangleSpawnPoint(terrain, groupCenter, i, columns, rows);
            GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.villageCenter = attackTarget;
            }
        }
    }

    private Vector3 GetRandomTerrainPoint(Terrain terrain)
    {
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;

        Vector3 randomPoint = new Vector3(
            Random.Range(terrainPosition.x, terrainPosition.x + terrainSize.x),
            0f,
            Random.Range(terrainPosition.z, terrainPosition.z + terrainSize.z)
        );

        randomPoint.y = terrain.SampleHeight(randomPoint) + terrainPosition.y + spawnHeightOffset;
        return randomPoint;
    }

    private Vector3 GetRectangleSpawnPoint(Terrain terrain, Vector3 groupCenter, int index, int columns, int rows)
    {
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;

        int row = index / columns;
        int column = index % columns;

        float offsetX = (column - (columns - 1) * 0.5f) * spacing;
        float offsetZ = (row - (rows - 1) * 0.5f) * spacing;

        Vector3 randomPoint = groupCenter + new Vector3(offsetX, 0f, offsetZ);

        randomPoint.x = Mathf.Clamp(randomPoint.x, terrainPosition.x, terrainPosition.x + terrainSize.x);
        randomPoint.z = Mathf.Clamp(randomPoint.z, terrainPosition.z, terrainPosition.z + terrainSize.z);
        randomPoint.y = terrain.SampleHeight(randomPoint) + terrainPosition.y + spawnHeightOffset;

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return randomPoint;
    }
}
