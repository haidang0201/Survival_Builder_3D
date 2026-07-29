namespace TopsonGames
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    public class ArmySpawner : MonoBehaviour
    {
        [Header("Spawn-Layout")]
        [Tooltip("Define the positions and the permitted unit types for each position here.")]
        public List<SpawnPoint> spawnPoints;
#if UNITY_EDITOR
        [ContextMenu("Execute Spawn Army")]
#endif
        public void SpawnArmy(InGameArmy armyToSpawn, bool isPlayer = false)
        {
            if (armyToSpawn == null)
            {
                return;
            }
            armyToSpawn.OnCheckSizes();

            List<SpawnPoint> availableSpawnPoints = new List<SpawnPoint>(spawnPoints);

            foreach (ArmyData armyData in armyToSpawn.armyData)
            {
                if (armyData.Unit == null || armyData.Unit.formationPrefab == null)
                {
                    continue;
                }
                SpawnPoint foundSpawnPoint = availableSpawnPoints.FirstOrDefault(sp =>
                    sp.transform != null &&
                    sp.allowedUnitTypes.Contains(armyData.Unit.unitType));

                if (foundSpawnPoint != null)
                {
                    GameObject formationGO = Instantiate(
                        armyData.Unit.formationPrefab,
                        foundSpawnPoint.transform.position,
                        foundSpawnPoint.transform.rotation
                    );

                    if (foundSpawnPoint.inverseForwardRotation)
                        formationGO.transform.rotation = foundSpawnPoint.transform.rotation * Quaternion.Euler(0, 180f, 0);
                    else
                        formationGO.transform.rotation = foundSpawnPoint.transform.rotation;

                    Formation formation = formationGO.GetComponent<Formation>();
                    formation.UnitData = armyData.Unit;
                    formation.armyData = armyData;
                    armyData.inGameFormation = formation;

                    if (isPlayer)
                        formation.TeamID = FormationController.instance.TeamID;
                    else
                        formation.TeamID = FormationController.instance.TeamID + 1; 


                    formation.CullUnitsToStrength(armyData.Troops);

                    formation.ArrangeFormation(false, false);
                    formation.MoveUnitsToWaypoints();
                    availableSpawnPoints.Remove(foundSpawnPoint);
                }
                else
                {
                    Debug.LogWarning($"No suitable and free spawn point for unit type '{armyData.Unit.unitType.name}' found. Please check avialable spawn points", this);
                }
            }
        }
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (spawnPoints == null)
                return;

            foreach (var sp in spawnPoints)
            {
                if (sp == null || sp.allowedUnitTypes == null)
                    continue;

                string allowedText = "Types: ";
                Vector3 offset = Vector3.zero;
                foreach (var ut in sp.allowedUnitTypes)
                {
                    if (ut == null)
                        continue;
                    allowedText += ut.name + " ";
                    Gizmos.color = ut.gizmosColor;
                    Gizmos.DrawLine(sp.transform.position + offset + sp.transform.right, sp.transform.position + sp.transform.right * -1f + offset);
                    Gizmos.DrawLine(sp.transform.position + offset + sp.transform.forward, sp.transform.position + sp.transform.forward * -1f + offset);
                    Gizmos.DrawLine(sp.transform.position + offset, sp.transform.position + sp.transform.up * 2f + offset);
                    Gizmos.DrawLine(sp.transform.position + offset + sp.transform.up * 2f, sp.transform.position + sp.transform.up * 2f + sp.transform.forward * 2f+ offset);

                    Gizmos.DrawLine(sp.transform.position + offset + sp.transform.up * 2f + sp.transform.forward * 2f, sp.transform.position + sp.transform.up * 2f + sp.transform.forward * 2f + sp.transform.right * 0.5f - sp.transform.forward * 0.5f + offset);
                    Gizmos.DrawLine(sp.transform.position + offset + sp.transform.up * 2f + sp.transform.forward * 2f, sp.transform.position + sp.transform.up * 2f + sp.transform.forward * 2f + sp.transform.right * -0.5f - sp.transform.forward * 0.5f + offset);
                    offset += Vector3.right * 0.1f;
                }
                Handles.Label(sp.transform.position, allowedText);
            }
        }
#endif
    }
}