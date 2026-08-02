namespace TopsonGames
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using TopsonGames.AI;
    using UnityEngine;
    using UnityEngine.AI;
    using UnityEngine.Events;
    using UnityEngine.EventSystems;
    using UnityEngine.InputSystem;

    [RequireComponent(typeof(EventSystem))]
    public class FormationController : MonoBehaviour
    {
        public enum GameMode
        {
            PlayerVsAI,
            Spectator_AIVsAI
        }

        [Header("Game Mode Settings")]
        [Tooltip("The current mode. Can be changed at runtime.")]
        public GameMode CurrentGameMode = GameMode.PlayerVsAI;
        [Tooltip("Reference to the AICommander that controls the player team (Team 0). Disabled in PlayerVsAI mode.")]
        public AICommander playerAICommander;

        [Header("Input")]
        public InputActionReference selectAction;
        public InputActionReference moveAction;
        public InputActionReference multiSelectAction;
        public InputActionReference UiCustomMultiSelectAction;
        public InputActionReference showWaypointsAction;
        public InputActionReference mousePositionAction;

        [Header("Detection")]
        public LayerMask groundMask;
        public LayerMask unitMask;
        [Tooltip("Layer for walls/defense zones.")]
        public LayerMask wallZoneMask;

        [Header("Player Team")]
        public int TeamID = 0;

        [Header("Highligting")]
        public bool showIndicatorOnHover = true;
        public bool showEnemyIndicator = false;
        public float hoverHideDelay = 0.3f;
        public bool showPathToWaypoint = true;
        public bool ShowVisualizersOnHighlight = false;
        public bool ShowVisualizersOnSelect = false;

        [Header("Selection Texture")]
        public Texture2D selectionBoxTexture;

        [Header("Formation Settings")]
        public int minFormationWidth = 2;
        public bool allowSingleRowFormations = false;
        public float maxSingleFormationWorldWidth = 100f;
        public float spacingBetweenFormations = 5f;
        public float minGroupScale = 0.5f;

        [Header("Combat Logic")]
        public float disengageCooldownDuration = 4f;

        [Header("Waypoint Parent")]
        public Transform waypointContainer;

        [Header("Debugging")]
        [SerializeField]
        List<Formation> AllFormations = new List<Formation>();
        [SerializeField]
        List<Formation> selectedFormations = new List<Formation>();

        private Formation lastHoveredFormation;
        private Dictionary<Formation, Coroutine> hideCoroutines = new Dictionary<Formation, Coroutine>();

        private Vector3 dragStartPos;
        private bool isDraggingRightMouse = false;
        private bool isDraggingLeftMouse = false;
        private Vector2 leftDragStartScreenPos;
        private List<Formation> selectionBeforeDrag = new List<Formation>();
        private struct TransformState { public Vector3 position; public Quaternion rotation; }
        private Dictionary<Transform, TransformState> originalWaypointStates = new Dictionary<Transform, TransformState>();

        private Dictionary<Formation, TransformState> dragTargetStates = new Dictionary<Formation, TransformState>();
        private Dictionary<Formation, int> dragFormationWidths = new Dictionary<Formation, int>();

        private Camera cam;
        public static FormationController instance;

        public UnityEvent OnDeselectFormations;

        private DefensiveZone currentSnapZone = null;

        void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(this);
            cam = Camera.main;
        }

        private void Start()
        {
            SetGameMode(CurrentGameMode);
        }

        private void OnEnable()
        {
            selectAction.action.Enable();
            moveAction.action.Enable();
            multiSelectAction.action.Enable();
            showWaypointsAction.action.Enable();
            mousePositionAction.action.Enable();
            UiCustomMultiSelectAction.action.Enable();
        }

        private void OnDisable()
        {
            selectAction.action.Disable();
            moveAction.action.Disable();
            multiSelectAction.action.Disable();
            showWaypointsAction.action.Disable();
            mousePositionAction.action.Disable();
            UiCustomMultiSelectAction.action.Disable();
        }

        void Update()
        {
            if (CurrentGameMode == GameMode.Spectator_AIVsAI) return;

            if (Application.isMobilePlatform) return;

            if (EventSystem.current.IsPointerOverGameObject()) return;
            HandleHoverEffect();
            HandleShowAllWaypoints();
            HandleSelection();
            HandleActions();
        }
        public void SetGameMode(GameMode newMode)
        {
            CurrentGameMode = newMode;

            if (CurrentGameMode == GameMode.Spectator_AIVsAI)
            {
                if (isDraggingRightMouse) CancelRightDrag();
                isDraggingLeftMouse = false;
                DeselectAllFormations();

                if (playerAICommander != null)
                {
                    playerAICommander.enabled = true;
                    Debug.Log("Spectator Mode: Player AI Activated.");
                }
            }
            else
            {
                if (playerAICommander != null)
                {
                    playerAICommander.enabled = false;
                    Debug.Log("Player Mode: Player AI Deactivated.");
                }
            }
        }

        void HandleShowAllWaypoints()
        {
            if (showWaypointsAction.action.WasPressedThisFrame()) ShowVisualizersForAllFormations(true);
            if (showWaypointsAction.action.WasReleasedThisFrame()) ShowVisualizersForAllFormations(false);
        }

        void ShowVisualizersForAllFormations(bool show)
        {
            foreach (Formation formation in AllFormations)
            {
                if (formation.TeamID == this.TeamID)
                {
                    if (show && showPathToWaypoint) formation.ShowVisualizers(true);
                    else formation.ShowVisualizers(false);

                    if (show) formation.ShowWaypointMarkers();
                    else formation.HideWaypointMarkers();
                }
            }
        }

        void HandleActions()
        {
            if (selectedFormations.Count == 0) return;

            if (moveAction.action.WasPressedThisFrame())
            {
                isDraggingRightMouse = false;
                currentSnapZone = null;

                Ray ray = cam.ScreenPointToRay(mousePositionAction.action.ReadValue<Vector2>());
                if (Physics.Raycast(ray, out RaycastHit zoneHit, 1000f, wallZoneMask))
                {
                    currentSnapZone = zoneHit.collider.GetComponent<DefensiveZone>() ?? zoneHit.collider.GetComponentInParent<DefensiveZone>();

                    if (currentSnapZone == null && zoneHit.collider.CompareTag("Wall"))
                    {
                        var zones = zoneHit.collider.GetComponentsInChildren<DefensiveZone>();
                        currentSnapZone = zones.OrderBy(z => Vector3.Distance(z.transform.position, zoneHit.point)).FirstOrDefault();
                    }
                }

                if (currentSnapZone == null)
                {
                    RaycastGround(out dragStartPos);
                }
            }

            if (moveAction.action.IsPressed())
            {
                // Check if first formation can man walls. 
                if (currentSnapZone != null && CanFormationManWalls(selectedFormations[0]))
                {
                    PreviewSnapToZone(currentSnapZone);
                }
                else if (RaycastGround(out Vector3 currentPos))
                {
                    if (!isDraggingRightMouse && (currentPos - dragStartPos).magnitude > 1f)
                    {
                        StartDragInternal();
                    }

                    if (isDraggingRightMouse)
                    {
                        if (selectedFormations.Count > 1) HandleMultiFormationDrag(currentPos);
                        else if (selectedFormations.Count == 1) HandleSingleFormationDrag(currentPos);
                    }
                }
            }

            if (moveAction.action.WasReleasedThisFrame())
            {
                if (currentSnapZone != null && CanFormationManWalls(selectedFormations[0]))
                {
                    ApplySnapToZone(currentSnapZone);
                    currentSnapZone = null;
                }
                else if (isDraggingRightMouse)
                {
                    EndDragInternal();
                }
                else
                {
                    Ray ray = cam.ScreenPointToRay(mousePositionAction.action.ReadValue<Vector2>());
                    if (Physics.Raycast(ray, out RaycastHit hit, 1000f, unitMask))
                    {
                        Formation clickedFormation = hit.collider.GetComponentInParent<Formation>();
                        if (clickedFormation != null && clickedFormation.TeamID != this.TeamID)
                        {
                            foreach (var f in selectedFormations) f?.SetCustomTarget(clickedFormation);
                            isDraggingRightMouse = false;
                            return;
                        }
                    }
                    if (RaycastGround(out Vector3 targetPoint))
                    {
                        MoveGroup(targetPoint);
                    }
                }
                isDraggingRightMouse = false;
            }
        }
        private void StartDragInternal()
        {
            isDraggingRightMouse = true;
            originalWaypointStates.Clear();
            dragTargetStates.Clear();
            dragFormationWidths.Clear();

            foreach (var f in selectedFormations)
            {
                if (f == null) continue;
                originalWaypointStates[f.WaypointCenter] = new TransformState { position = f.WaypointCenter.position, rotation = f.WaypointCenter.rotation };
                dragTargetStates[f] = new TransformState { position = f.WaypointCenter.position, rotation = f.WaypointCenter.rotation };
                dragFormationWidths[f] = f.formationWidth;

                f.HideWaypoints();
                f.ShowWaypointIndicators();
                f.ShowVisualizers(false);
            }
        }

        private void EndDragInternal()
        {
            originalWaypointStates.Clear();
            foreach (var f in selectedFormations)
            {
                if (f == null) continue;
                f.HideWaypointIndicators();
                var targetState = dragTargetStates[f];
                var targetWidth = dragFormationWidths[f];

                f.WaypointCenter.position = targetState.position;
                f.WaypointCenter.rotation = targetState.rotation;
                f.formationWidth = targetWidth;

                f.SetMoveOrder();
                if (ShowVisualizersOnSelect && selectedFormations.Contains(f)) f.ShowVisualizers(true);
            }
            dragTargetStates.Clear();
            dragFormationWidths.Clear();
        }

        bool CanFormationManWalls(Formation f)
        {
            if (f == null || f.UnitData == null || f.UnitData.unitType == null) return false;
            return f.UnitData.unitType.canManWalls;
        }

        private Dictionary<Formation, DefensiveZone> DistributeFormationsToZones(DefensiveZone startZone)
        {
            Dictionary<Formation, DefensiveZone> assignments = new Dictionary<Formation, DefensiveZone>();
            List<Formation> availableFormations = new List<Formation>();

            foreach (var f in selectedFormations)
            {
                if (CanFormationManWalls(f)) availableFormations.Add(f);
            }
            if (availableFormations.Count == 0) return assignments;

            availableFormations.Sort((a, b) =>
                Vector3.Distance(a.transform.position, startZone.transform.position)
                .CompareTo(Vector3.Distance(b.transform.position, startZone.transform.position)));

            Queue<DefensiveZone> zoneQueue = new Queue<DefensiveZone>();
            HashSet<DefensiveZone> visitedZones = new HashSet<DefensiveZone>();

            zoneQueue.Enqueue(startZone);
            visitedZones.Add(startZone);

            int formationIndex = 0;

            while (zoneQueue.Count > 0 && formationIndex < availableFormations.Count)
            {
                DefensiveZone currentZone = zoneQueue.Dequeue();


                assignments.Add(availableFormations[formationIndex], currentZone);
                formationIndex++;


                if (currentZone.neighbors != null)
                {
                    var sortedNeighbors = currentZone.neighbors
                        .OrderBy(n => Vector3.Distance(n.transform.position, startZone.transform.position))
                        .ToList();

                    foreach (var neighbor in sortedNeighbors)
                    {
                        if (neighbor != null && !visitedZones.Contains(neighbor))
                        {
                            visitedZones.Add(neighbor);
                            zoneQueue.Enqueue(neighbor);
                        }
                    }
                }
            }

            return assignments;
        }

        void PreviewSnapToZone(DefensiveZone zone)
        {
            if (selectedFormations.Count == 0) return;

            var assignments = DistributeFormationsToZones(zone);

            foreach (var kvp in assignments)
            {
                Formation f = kvp.Key;
                DefensiveZone assignedZone = kvp.Value;
                SnapVisualsForOneFormation(f, assignedZone);
            }

            foreach (var f in selectedFormations)
            {
                if (!assignments.ContainsKey(f))
                {
                    f.HideWaypointIndicators();
                }
            }
        }

        void SnapVisualsForOneFormation(Formation formation, DefensiveZone zone)
        {
            float unitSpacing = Mathf.Max(formation.UnitData.formationSpacing, 0.1f);
            int targetWidth = Mathf.FloorToInt(zone.zoneWidth / unitSpacing) + 1;

            int numUnits = formation.numberOfUnits;
            int minW = minFormationWidth;
            int maxW = allowSingleRowFormations ? numUnits : Mathf.Max(1, numUnits - 1);
            if (numUnits < minW) minW = numUnits;
            if (maxW < minW) maxW = minW;
            if (targetWidth < 1) targetWidth = 1;
            int clampedWidth = Mathf.Clamp(targetWidth, minW, maxW);

            formation.ShowWaypointIndicators();
            formation.UpdateDragVisuals(zone.transform.position, zone.transform.rotation, clampedWidth);
        }

        void ApplySnapToZone(DefensiveZone zone)
        {
            if (selectedFormations.Count == 0) return;

            var assignments = DistributeFormationsToZones(zone);

            foreach (var kvp in assignments)
            {
                Formation f = kvp.Key;
                DefensiveZone assignedZone = kvp.Value;

                float unitSpacing = Mathf.Max(f.UnitData.formationSpacing, 0.1f);
                int targetWidth = Mathf.FloorToInt(assignedZone.zoneWidth / unitSpacing) + 1;
                int numUnits = f.numberOfUnits;
                int minW = minFormationWidth;
                int maxW = allowSingleRowFormations ? numUnits : Mathf.Max(1, numUnits - 1);
                if (numUnits < minW) minW = numUnits;
                if (maxW < minW) maxW = minW;
                if (targetWidth < 1) targetWidth = 1;
                int clampedWidth = Mathf.Clamp(targetWidth, minW, maxW);

                f.formationWidth = clampedWidth;
                f.WaypointCenter.position = assignedZone.transform.position;
                f.WaypointCenter.rotation = assignedZone.transform.rotation;

                f.HideWaypointIndicators();
                f.SetMoveOrder();

                if (assignedZone.IsOccupied() && assignedZone.GetOccupier() != f)
                {
                    // Optional: Kick logic. 
                }
                assignedZone.SetOccupier(f);

                if (ShowVisualizersOnSelect) f.ShowVisualizers(true);
            }

            List<Formation> overflowFormations = new List<Formation>();
            foreach (var f in selectedFormations)
            {
                if (!assignments.ContainsKey(f))
                {
                    overflowFormations.Add(f);
                }
            }

            if (overflowFormations.Count > 0)
            {
                Vector3 fallbackPos = zone.transform.position - zone.transform.forward * 5f;

                foreach (var f in overflowFormations)
                {
                    f.WaypointCenter.position = fallbackPos;
                    f.SetMoveOrder();
                    fallbackPos -= f.transform.right * 5f;
                }
            }
        }
        void MoveGroup(Vector3 targetPoint)
        {
            var groupWaypointCenter = Vector3.zero;
            int validCount = 0;
            if (selectedFormations.Count > 0)
            {
                foreach (var f in selectedFormations)
                {
                    if (f != null)
                    {
                        groupWaypointCenter += f.transform.position;
                        validCount++;
                    }
                }
                if (validCount > 0) groupWaypointCenter /= validCount;
                else return;
            }
            else return;

            foreach (var f in selectedFormations)
            {
                if (f == null || f.WaypointCenter == null) continue;
                var offset = f.transform.position - groupWaypointCenter;
                var finalPos = targetPoint + offset;
                f.WaypointCenter.position = finalPos;

                Vector3 moveDirection = (finalPos - f.transform.position).normalized;
                if (moveDirection != Vector3.zero)
                {
                    f.WaypointCenter.rotation = Quaternion.LookRotation(moveDirection);
                }

                f.FlashWaypointIndicators();
                f.SetMoveOrder();
                if (ShowVisualizersOnSelect && selectedFormations.Contains(f))
                {
                    f.ShowVisualizers(true);
                }
            }
        }

        void HandleSingleFormationDrag(Vector3 currentPos)
        {
            Formation formation = selectedFormations[0];
            Vector3 dragVector = currentPos - dragStartPos;
            float dragDistance = dragVector.magnitude;

            if (dragDistance > maxSingleFormationWorldWidth)
            {
                dragDistance = maxSingleFormationWorldWidth;
                dragVector = dragVector.normalized * dragDistance;
            }

            if (dragDistance < 0.1f) return;

            float angle = Mathf.Atan2(dragVector.x, dragVector.z) * Mathf.Rad2Deg - 90f;
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            Vector3 newCenter = dragStartPos + (dragVector / 2f);

            int numUnits = formation.numberOfUnits;
            int calculatedWidth = Mathf.RoundToInt(dragDistance / Mathf.Abs(formation.UnitData.formationSpacing));
            int minW = minFormationWidth;
            int maxW = allowSingleRowFormations ? numUnits : numUnits - 1;

            if (numUnits < minW) minW = numUnits;
            if (maxW < minW) maxW = minW;
            if (calculatedWidth < 1) calculatedWidth = 1;

            int newFormationWidth = Mathf.Clamp(calculatedWidth, minW, maxW);

            dragTargetStates[formation] = new TransformState { position = newCenter, rotation = rotation };
            dragFormationWidths[formation] = newFormationWidth;
            formation.UpdateDragVisuals(newCenter, rotation, newFormationWidth);
        }

        void HandleMultiFormationDrag(Vector3 currentPos)
        {
            var dragVector = currentPos - dragStartPos;
            var dragDistance = dragVector.magnitude;
            if (dragDistance < 1f) return;

            Vector3 cameraRight = cam.transform.right;
            selectedFormations.Sort((a, b) =>
            {
                float posA = Vector3.Dot(a.transform.position, cameraRight);
                float posB = Vector3.Dot(b.transform.position, cameraRight);
                return posA.CompareTo(posB);
            });

            int formationCount = selectedFormations.Count;
            if (formationCount > 1)
            {
                float maxGroupDragDistance = (formationCount * maxSingleFormationWorldWidth) + ((formationCount - 1) * spacingBetweenFormations);
                if (dragDistance > maxGroupDragDistance)
                {
                    dragDistance = maxGroupDragDistance;
                    dragVector = dragVector.normalized * dragDistance;
                }
            }

            var lineDirection = dragVector.normalized;
            var angle = Mathf.Atan2(dragVector.x, dragVector.z) * Mathf.Rad2Deg - 90f;
            var groupRotation = Quaternion.Euler(0, angle, 0);

            var initialTotalWorldWidth = 0f;
            var initialFormationWorldWidths = new List<float>();
            foreach (var f in selectedFormations)
            {
                int w = f.targetFormationWidth > 0 ? f.targetFormationWidth : f.formationWidth;
                if (w < minFormationWidth) w = minFormationWidth;

                var worldWidth = (f.UnitData.formationSpacing > 0) ? (w - 1) * Mathf.Abs(f.UnitData.formationSpacing) : 1f;
                if (worldWidth < 1f) worldWidth = 1f;

                initialFormationWorldWidths.Add(worldWidth);
                initialTotalWorldWidth += worldWidth;
            }

            if (formationCount > 1) initialTotalWorldWidth += (formationCount - 1) * this.spacingBetweenFormations;
            if (initialTotalWorldWidth < 1f) initialTotalWorldWidth = 1f;

            var scaleFactor = dragDistance / initialTotalWorldWidth;
            scaleFactor = Mathf.Max(scaleFactor, minGroupScale);

            var currentPlacementPos = dragStartPos;
            for (int i = 0; i < selectedFormations.Count; i++)
            {
                var f = selectedFormations[i];
                var initialWorldWidth = initialFormationWorldWidths[i];
                var scaledWorldWidth = initialWorldWidth * scaleFactor;
                var scaledSpacing = (i < selectedFormations.Count - 1) ? this.spacingBetweenFormations * scaleFactor : 0;
                var formationCenter = currentPlacementPos + lineDirection * (scaledWorldWidth / 2f);

                int numUnits = f.numberOfUnits;

                float spacingAbs = Mathf.Max(Mathf.Abs(f.UnitData.formationSpacing), 0.01f);
                int newFormationWidthInt = Mathf.RoundToInt(scaledWorldWidth / spacingAbs) + 1;

                int minW = minFormationWidth;
                int maxW = allowSingleRowFormations ? numUnits : numUnits - 1;
                if (numUnits < minW) minW = numUnits;
                if (maxW < minW) maxW = minW;
                if (newFormationWidthInt < 1) newFormationWidthInt = 1;

                int newFormationWidth = Mathf.Clamp(newFormationWidthInt, minW, maxW);

                dragTargetStates[f] = new TransformState { position = formationCenter, rotation = groupRotation };
                dragFormationWidths[f] = newFormationWidth;
                f.UpdateDragVisuals(formationCenter, groupRotation, newFormationWidth);

                currentPlacementPos += lineDirection * (scaledWorldWidth + scaledSpacing);
            }
        }

        void CancelRightDrag()
        {
            if (!isDraggingRightMouse) return;

            foreach (var f in selectedFormations)
            {
                f.HideWaypointIndicators();
                if (ShowVisualizersOnSelect && selectedFormations.Contains(f))
                {
                    f.ShowVisualizers(true);
                }
            }

            isDraggingRightMouse = false;
            originalWaypointStates.Clear();
            dragTargetStates.Clear();
            dragFormationWidths.Clear();
        }

        void HandleSelection()
        {
            if (selectAction.action.WasPressedThisFrame())
            {
                if (isDraggingRightMouse)
                {
                    CancelRightDrag();
                    DeselectAllFormations();
                    return;
                }

                isDraggingLeftMouse = false;
                leftDragStartScreenPos = mousePositionAction.action.ReadValue<Vector2>();

                selectionBeforeDrag.Clear();
                selectionBeforeDrag.AddRange(selectedFormations);
            }

            if (selectAction.action.IsPressed())
            {
                if (!isDraggingLeftMouse && (leftDragStartScreenPos - mousePositionAction.action.ReadValue<Vector2>()).magnitude > 10f)
                {
                    isDraggingLeftMouse = true;
                    if (!multiSelectAction.action.IsPressed())
                    {
                        DeselectAllFormations();
                        selectionBeforeDrag.Clear();
                    }
                }

                if (isDraggingLeftMouse)
                {
                    UpdateBoxSelection();
                }
            }

            if (selectAction.action.WasReleasedThisFrame())
            {
                if (isDraggingLeftMouse) { }
                else
                {
                    SelectFormationByClick();
                }
                isDraggingLeftMouse = false;
            }
        }

        void UpdateBoxSelection()
        {
            Rect selectionRect = GetScreenRect(leftDragStartScreenPos, mousePositionAction.action.ReadValue<Vector2>());

            foreach (var formation in AllFormations)
            {
                if (formation.TeamID != this.TeamID) continue;

                bool isInBox = IsFormationInBox(formation, selectionRect);
                bool isSelected = selectedFormations.Contains(formation);
                bool wasSelectedBeforeDrag = selectionBeforeDrag.Contains(formation);

                if (isInBox)
                {
                    if (!isSelected)
                    {
                        selectedFormations.Add(formation);
                        formation.ShowSelectionIndicators();
                        if (ShowVisualizersOnSelect)
                        {
                            formation.ShowVisualizers(true);
                        }
                    }
                }
                else
                {
                    if (isSelected && !wasSelectedBeforeDrag)
                    {
                        selectedFormations.Remove(formation);
                        formation.HideSelectionIndicators();
                        if (ShowVisualizersOnSelect)
                        {
                            formation.ShowVisualizers(false);
                        }
                    }
                }
            }
        }

        bool IsFormationInBox(Formation formation, Rect selectionRect)
        {
            foreach (var unit in formation.GetUnits())
            {
                if (unit == null) continue;
                Vector3 screenPos = cam.WorldToScreenPoint(unit.transform.position);
                if (screenPos.z > 0 && selectionRect.Contains(screenPos))
                {
                    return true;
                }
            }
            return false;
        }

        void SelectFormationByClick()
        {
            bool isShiftHeld = multiSelectAction.action.IsPressed();
            if (!isShiftHeld)
            {
                DeselectAllFormations();
            }

            Ray ray = cam.ScreenPointToRay(mousePositionAction.action.ReadValue<Vector2>());
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, unitMask))
            {
                Formation clickedFormation = hit.collider.GetComponentInParent<Formation>();
                if (clickedFormation != null && clickedFormation.TeamID == this.TeamID)
                {
                    ToggleFormationSelection(clickedFormation);
                }
            }
        }

        void DeselectAllFormations()
        {
            foreach (var coroutine in hideCoroutines.Values)
            {
                StopCoroutine(coroutine);
            }
            hideCoroutines.Clear();

            if (lastHoveredFormation != null)
            {
                if (!selectedFormations.Contains(lastHoveredFormation))
                {
                    lastHoveredFormation.HideSelectionIndicators();
                    if (ShowVisualizersOnHighlight && (lastHoveredFormation.TeamID != this.TeamID && showEnemyIndicator || lastHoveredFormation.TeamID == this.TeamID))
                    {
                        lastHoveredFormation.ShowVisualizers(false);
                    }
                }
                lastHoveredFormation = null;
            }

            foreach (var f in selectedFormations)
            {
                f.HideSelectionIndicators();
                if (ShowVisualizersOnSelect)
                {
                    f.ShowVisualizers(false);
                }
            }
            selectedFormations.Clear();
            OnDeselectFormations?.Invoke();
        }

        public void RegisterFormation(Formation formation)
        {
            if (!AllFormations.Contains(formation)) AllFormations.Add(formation);
        }

        public void RemoveFormation(Formation formation)
        {
            if (AllFormations.Contains(formation)) AllFormations.Remove(formation);

            if (selectedFormations.Contains(formation)) selectedFormations.Remove(formation);
        }

        void HandleHoverEffect()
        {
            if (!showIndicatorOnHover)
            {
                if (lastHoveredFormation != null && !selectedFormations.Contains(lastHoveredFormation))
                {
                    lastHoveredFormation.HideSelectionIndicators();
                    if (hideCoroutines.ContainsKey(lastHoveredFormation))
                    {
                        StopCoroutine(hideCoroutines[lastHoveredFormation]);
                        hideCoroutines.Remove(lastHoveredFormation);
                    }
                    if (ShowVisualizersOnHighlight && (lastHoveredFormation.TeamID != this.TeamID && showEnemyIndicator || lastHoveredFormation.TeamID == this.TeamID))
                    {
                        lastHoveredFormation.ShowVisualizers(false);
                    }
                }
                lastHoveredFormation = null;
                return;
            }

            Ray ray = cam.ScreenPointToRay(mousePositionAction.action.ReadValue<Vector2>());
            Formation currentlyHoveredFormation = null;
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, unitMask))
            {
                currentlyHoveredFormation = hit.collider.GetComponentInParent<Formation>();
            }

            if (currentlyHoveredFormation != lastHoveredFormation)
            {
                if (lastHoveredFormation != null)
                {
                    if (hideCoroutines.ContainsKey(lastHoveredFormation))
                    {
                        StopCoroutine(hideCoroutines[lastHoveredFormation]);
                        hideCoroutines.Remove(lastHoveredFormation);
                    }
                    Coroutine hideRoutine = StartCoroutine(HideIndicatorAfterDelay(lastHoveredFormation, hoverHideDelay));
                    hideCoroutines.Add(lastHoveredFormation, hideRoutine);
                }

                if (currentlyHoveredFormation != null)
                {
                    bool isEnemy = currentlyHoveredFormation.TeamID != this.TeamID;

                    if (isEnemy && !showEnemyIndicator)
                    {
                        lastHoveredFormation = currentlyHoveredFormation;
                        return;
                    }

                    if (hideCoroutines.ContainsKey(currentlyHoveredFormation))
                    {
                        StopCoroutine(hideCoroutines[currentlyHoveredFormation]);
                        hideCoroutines.Remove(currentlyHoveredFormation);
                    }

                    if (!selectedFormations.Contains(currentlyHoveredFormation))
                    {
                        currentlyHoveredFormation.ShowSelectionIndicators();
                        UiManager.Instance.OnHoverUI(true, currentlyHoveredFormation);
                    }
                    if (ShowVisualizersOnHighlight)
                    {
                        currentlyHoveredFormation.ShowVisualizers(true);
                    }
                }
                lastHoveredFormation = currentlyHoveredFormation;
            }
        }

        IEnumerator HideIndicatorAfterDelay(Formation formation, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (formation != null && !selectedFormations.Contains(formation) && lastHoveredFormation != formation)
            {
                formation.HideSelectionIndicators();
                UiManager.Instance.OnHoverUI(false, null);
                if (ShowVisualizersOnHighlight && (formation.TeamID != this.TeamID && showEnemyIndicator || formation.TeamID == this.TeamID))
                {
                    formation.ShowVisualizers(false);
                }
            }

            if (hideCoroutines.ContainsKey(formation))
            {
                hideCoroutines.Remove(formation);
            }
        }

        bool IsPointInNoSpawnZone(Vector3 position)
        {
            float checkRadius = 1.0f;
            Collider[] hits = Physics.OverlapSphere(position, checkRadius);

            foreach (var hit in hits)
            {
                if (hit.GetComponent<NoPlacementZone>() != null)
                {
                    return true;
                }
            }
            return false;
        }

        void OnGUI()
        {
            if (CurrentGameMode == GameMode.Spectator_AIVsAI) return;

            if (isDraggingLeftMouse)
            {
                var rect = GetScreenRectForGUI(leftDragStartScreenPos, mousePositionAction.action.ReadValue<Vector2>());
                if (selectionBoxTexture != null)
                    GUI.DrawTexture(rect, selectionBoxTexture);
                else
                {
                    GUI.backgroundColor = new Color(0.8f, 0.8f, 1f, 0.25f);
                    GUI.Box(rect, "");
                    GUI.backgroundColor = Color.white;
                }
            }
        }

        bool RaycastGround(out Vector3 point)
        {
            Vector2 screenPos = mousePositionAction.action.ReadValue<Vector2>();
            if (Application.isMobilePlatform && Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
            {
                screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
            }

            Ray ray = cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask))
            {
                if (IsPointInNoSpawnZone(hit.point))
                {
                    point = Vector3.zero;
                    return false;
                }

                if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
                {
                    point = navHit.position;
                    return true;
                }
            }
            point = Vector3.zero;
            return false;
        }

        bool RaycastGroundFromPos(Vector2 screenPos, out Vector3 point)
        {
            Ray ray = cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask))
            {
                if (IsPointInNoSpawnZone(hit.point))
                {
                    point = Vector3.zero;
                    return false;
                }

                if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
                {
                    point = navHit.position;
                    return true;
                }
            }
            point = Vector3.zero;
            return false;
        }

        private Rect GetScreenRect(Vector2 screenPos1, Vector2 screenPos2)
        {
            var topLeft = Vector2.Min(screenPos1, screenPos2);
            var bottomRight = Vector2.Max(screenPos1, screenPos2);
            return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
        }

        private Rect GetScreenRectForGUI(Vector2 screenPos1, Vector2 screenPos2)
        {
            screenPos1.y = Screen.height - screenPos1.y;
            screenPos2.y = Screen.height - screenPos2.y;
            var topLeft = Vector2.Min(screenPos1, screenPos2);
            var bottomRight = Vector2.Max(screenPos1, screenPos2);
            return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
        }

        void SelectSingleFormation(Formation f)
        {
            DeselectAllFormations();
            selectedFormations.Add(f);
            f.ShowSelectionIndicators();
            if (ShowVisualizersOnSelect)
            {
                f.ShowVisualizers(true);
            }
        }

        void ToggleFormationSelection(Formation f)
        {
            if (selectedFormations.Contains(f))
            {
                selectedFormations.Remove(f);
                f.HideSelectionIndicators();
                if (ShowVisualizersOnSelect)
                {
                    f.ShowVisualizers(false);
                }
            }
            else
            {
                selectedFormations.Add(f);
                f.ShowSelectionIndicators();
                if (ShowVisualizersOnSelect)
                {
                    f.ShowVisualizers(true);
                }
            }
        }
        public List<Formation> GetFormations()
        {
            return AllFormations;
        }
        public List<Formation> GetSelectedFormations()
        {
            return selectedFormations;
        }
        public void ClearSelectedFormations()
        {
            foreach (var formation in selectedFormations)
            {
                if (formation == null)
                    continue;
                formation.ShowVisualizers(false);
                formation.HideSelectionIndicators();
            }
            selectedFormations.Clear();
        }
        public void AddSelectedFormation(Formation formation)
        {
            if (formation == null || formation.TeamID != TeamID || selectedFormations.Contains(formation))
                return;
            selectedFormations.Add(formation);

            formation.ShowSelectionIndicators();
            if (ShowVisualizersOnSelect)
            {
                formation.ShowVisualizers(true);
            }
        }

        #region MOBILE SUPPORT

        public void DeselectAllFormationsPublic()
        {
            DeselectAllFormations();
        }
        public void Mobile_UpdateBoxSelection(Vector2 start, Vector2 end)
        {
            if (CurrentGameMode == GameMode.Spectator_AIVsAI) return;
            Rect selectionRect = GetScreenRect(start, end);

            foreach (var formation in AllFormations)
            {
                if (formation.TeamID != this.TeamID) continue;

                bool isInBox = IsFormationInBox(formation, selectionRect);
                bool isSelected = selectedFormations.Contains(formation);

                if (isInBox)
                {
                    if (!isSelected)
                    {
                        selectedFormations.Add(formation);
                        formation.ShowSelectionIndicators();
                        if (ShowVisualizersOnSelect) formation.ShowVisualizers(true);
                    }
                }
            }
        }

        public bool Mobile_TryStartFormationDrag(Vector2 screenPos)
        {
            if (CurrentGameMode == GameMode.Spectator_AIVsAI) return false;
            if (RaycastGroundFromPos(screenPos, out Vector3 hitPoint))
            {
                dragStartPos = hitPoint;
                StartDragInternal();
                return true;
            }
            return false;
        }

        public void Mobile_UpdateFormationDrag(Vector2 screenPos)
        {
            if (CurrentGameMode == GameMode.Spectator_AIVsAI) return;
            if (RaycastGroundFromPos(screenPos, out Vector3 currentPos))
            {
                if (selectedFormations.Count > 1) HandleMultiFormationDrag(currentPos);
                else if (selectedFormations.Count == 1) HandleSingleFormationDrag(currentPos);
            }
        }

        public void Mobile_EndFormationDrag()
        {
            if (CurrentGameMode == GameMode.Spectator_AIVsAI) return;
            EndDragInternal();
            isDraggingRightMouse = false;
        }

        public void Mobile_HandleTap(Vector2 screenPos)
        {
            if (CurrentGameMode == GameMode.Spectator_AIVsAI) return;
            Ray ray = cam.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out RaycastHit hitUnit, 1000f, unitMask))
            {
                Formation clickedFormation = hitUnit.collider.GetComponentInParent<Formation>();
                if (clickedFormation != null && clickedFormation.TeamID != this.TeamID)
                {
                    foreach (var f in selectedFormations) f?.SetCustomTarget(clickedFormation);
                    return;
                }

                if (clickedFormation != null && clickedFormation.TeamID == this.TeamID)
                {
                    ToggleFormationSelection(clickedFormation);
                    return;
                }
            }

            if (Physics.Raycast(ray, out RaycastHit hitGround, 1000f, groundMask))
            {
                if (selectedFormations.Count > 0)
                {
                    MoveGroup(hitGround.point);
                }
                else
                {
                    DeselectAllFormations();
                }
            }
            else
            {
                DeselectAllFormations();
            }
        }

        #endregion
    }
}