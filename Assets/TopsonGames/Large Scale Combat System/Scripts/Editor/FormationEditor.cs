namespace TopsonGames
{
    using Unity.VisualScripting;
    using UnityEditor;
    using UnityEngine;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(Formation))]
    public class FormationEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Formation formation = (Formation)target;
            if (GUILayout.Button("Create Formation"))
            {
                Undo.RecordObject(formation, "Create Formation from Children");
                if (formation.WaypointCenter == null)
                {
                    GameObject wcGO = new GameObject("Waypoint Center");
                    Undo.RegisterCreatedObjectUndo(wcGO, "Create Waypoint Center");
                    wcGO.transform.SetParent(formation.transform);
                    wcGO.transform.localPosition = Vector3.zero;
                    wcGO.transform.localRotation = Quaternion.identity;
                    formation.WaypointCenter = wcGO.transform;
                }
                if (formation.WaypointIndicator == null)
                {
                    GameObject wiGO = new GameObject("Waypoint Indicator Parent");
                    Undo.RegisterCreatedObjectUndo(wiGO, "Create Waypoint Indicator Parent");
                    wiGO.transform.SetParent(formation.transform);
                    wiGO.transform.localPosition = Vector3.zero;
                    wiGO.transform.localRotation = Quaternion.identity;
                    formation.WaypointIndicator = wiGO.transform;
                }

                Unit[] unitsToMove = formation.GetComponentsInChildren<Unit>();
                foreach (var unit in unitsToMove)
                {
                    Undo.RecordObject(unit.transform, "Position Unit in Formation");
                    unit.tag = "Unit";
                    if (unit.shieldCollider != null)
                        unit.shieldCollider.tag = "Shield";
                }
                formation.formationWidth = formation.UnitData.formationWidth;
                formation.numberOfUnits = unitsToMove.Length;
                if (formation.UnitData)
                {

                    formation.UnitData.Troops = unitsToMove.Length;
                    formation.FindAndArrangeUnitsFromChildren();
                }
                else
                    Debug.Log("Assign Unit Data" + " " + formation.gameObject);
               

                if (GUI.changed)
                {
                    EditorUtility.SetDirty(formation);
                    EditorUtility.SetDirty(formation.UnitData);
                }
                Debug.Log("Formation was created with " + formation.UnitData.Troops + " Units. Name : " + formation.gameObject.name);
            }
        }

    }
}