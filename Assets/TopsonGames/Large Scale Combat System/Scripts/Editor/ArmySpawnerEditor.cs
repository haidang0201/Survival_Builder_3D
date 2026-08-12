namespace TopsonGames
{
    using UnityEngine;
    using UnityEditor;
    using UnityEngine.AI;

    [CustomEditor(typeof(ArmySpawner))]
    public class ArmySpawnerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ArmySpawner spawner = (ArmySpawner)target;
            GUILayout.Space(15);

            if (GUILayout.Button("Snap Spawn Points to NavMesh"))
            {
                SnapToNavMesh(spawner);
            }
        }

        private void SnapToNavMesh(ArmySpawner spawner)
        {
            if (spawner.spawnPoints == null || spawner.spawnPoints.Count == 0)
            {
                Debug.Log("No spawn points available for customization.", spawner);
                return;
            }

            int snappedCount = 0;
            foreach (var spawnPoint in spawner.spawnPoints)
            {
                if (spawnPoint != null)
                {
                    Undo.RecordObject(spawnPoint.transform, "Snap to NavMesh");
                    if (NavMesh.SamplePosition(spawnPoint.transform.position, out NavMeshHit hit, 20f, NavMesh.AllAreas))
                    {
                        spawnPoint.transform.position = hit.position;
                        snappedCount++;
                    }
                    else
                    {
                        Debug.LogWarning($"Could not find a NavMesh point near '{spawnPoint.transform.name}'.", spawnPoint.transform);
                    }
                }
            }

            if (snappedCount > 0)
            {
                Debug.Log($"{snappedCount} Spawn points have been adapted to the NavMesh.", spawner);
            }
        }
    }
}