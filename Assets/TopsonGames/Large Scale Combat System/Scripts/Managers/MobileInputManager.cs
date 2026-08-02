namespace TopsonGames
{
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.UI;
    using UnityEngine.EventSystems;

    using UnityEngine.InputSystem.EnhancedTouch;
    using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

    public class MobileInputManager : MonoBehaviour
    {
        public static MobileInputManager Instance;

        [Header("References")]
        public FormationController formationController;
        public CameraController cameraController;

        [Header("UI References")]
        public Button selectionModeToggleBtn;
        public Color selectedColor = Color.green;
        public Button deselectButton;
        public Image selectionBoxVisual;

        [Header("Settings")]
        public float longPressTime = 0.5f;
        public float panSensitivity = 0.05f;
        public float zoomSensitivity = 0.01f;
        public float rotationSensitivity = 0.4f;
        public float dragThreshold = 20f;

        [Header("Input Actions")]
        public InputActionReference touchPositionAction;
        public InputActionReference touchContactAction;

        private bool isSelectionMode = false;
        private bool isDragging = false;
        private bool isLongPressTriggered = false;
        private float touchTimer;

        private Vector2 startTouchPos;
        private Vector2 lastTouchPos;
        private bool touchStartedOnUI = false;

        private float initialPinchDistance;
        private float lastPinchAngle;
        private bool isMultiTouch = false;

        private RectTransform selectionBoxParent;
        private Canvas rootCanvas;

        private void Awake()
        {
            if (Instance == null) Instance = this;

            if (selectionBoxVisual)
            {
                selectionBoxVisual.gameObject.SetActive(false);
                RectTransform rt = selectionBoxVisual.rectTransform;
                rt.pivot = Vector2.zero;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.zero;
                selectionBoxParent = rt.parent as RectTransform;
                rootCanvas = selectionBoxVisual.canvas;
            }

            if (selectionModeToggleBtn)
                selectionModeToggleBtn.onClick.AddListener(ToggleSelectionMode);

            if (deselectButton)
            {
                deselectButton.onClick.AddListener(DeselectAll);
                deselectButton.gameObject.SetActive(false);
            }

            UpdateUI();
        }

        private void Start()
        {
            if (!formationController) formationController = FindAnyObjectByType<FormationController>();
            if (!cameraController) cameraController = FindAnyObjectByType<CameraController>();
        }

        private void OnEnable()
        {
            touchPositionAction.action.Enable();
            touchContactAction.action.Enable();
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            touchPositionAction.action.Disable();
            touchContactAction.action.Disable();
            EnhancedTouchSupport.Disable();
        }

        private void Update()
        {
            UpdateDeselectButtonState();

            if (ETouch.activeTouches.Count == 0 && !Mouse.current.leftButton.isPressed)
            {
                ResetState();
                return;
            }

            int touchCount = ETouch.activeTouches.Count;
            if (touchCount == 0 && Mouse.current.leftButton.isPressed) touchCount = 1;

            Vector2 currentPos = touchPositionAction.action.ReadValue<Vector2>();

            // 1 Finger Logic
            if (touchCount == 1)
            {
                isMultiTouch = false;

                if (touchContactAction.action.WasPressedThisFrame())
                {
                    touchStartedOnUI = IsPointerOverUI();
                    startTouchPos = currentPos;
                    lastTouchPos = currentPos;
                    touchTimer = 0f;
                    isDragging = false;
                    isLongPressTriggered = false;
                }

                if (touchStartedOnUI) return;

                touchTimer += Time.deltaTime;
                float dragDist = Vector2.Distance(currentPos, startTouchPos);

                if (!isDragging && dragDist > dragThreshold)
                {
                    isDragging = true;
                }

                if (isSelectionMode)
                {
                    if (isDragging)
                    {
                        UpdateSelectionBoxUI(startTouchPos, currentPos);
                        formationController.Mobile_UpdateBoxSelection(startTouchPos, currentPos);
                    }
                }
                else
                {
                    // Long Press Detection for Formation Drag
                    if (!isDragging && !isLongPressTriggered && touchTimer > longPressTime)
                    {
                        if (formationController.GetSelectedFormations().Count > 0)
                        {
                            if (formationController.Mobile_TryStartFormationDrag(startTouchPos))
                            {
                                isLongPressTriggered = true;
                            }
                        }
                    }

                    if (isLongPressTriggered)
                    {
                        formationController.Mobile_UpdateFormationDrag(currentPos);
                    }
                    else if (isDragging)
                    {
                        Vector2 delta = currentPos - lastTouchPos;
                        cameraController.Mobile_PanCamera(delta * panSensitivity);
                    }
                }

                if (touchContactAction.action.WasReleasedThisFrame())
                {
                    if (selectionBoxVisual) selectionBoxVisual.gameObject.SetActive(false);

                    if (isLongPressTriggered)
                    {
                        formationController.Mobile_EndFormationDrag();
                        isLongPressTriggered = false;
                    }
                    else if (!isDragging && !touchStartedOnUI)
                    {
                        HandleTap(currentPos);
                    }
                    touchStartedOnUI = false;
                }

                lastTouchPos = currentPos;
            }
            // 2 Finger Logic (Zoom & Rotate)
            else if (touchCount >= 2)
            {
                if (ETouch.activeTouches.Count < 2) return;

                var t1 = ETouch.activeTouches[0];
                var t2 = ETouch.activeTouches[1];

                float currentPinchDist = Vector2.Distance(t1.screenPosition, t2.screenPosition);
                Vector2 diff = t2.screenPosition - t1.screenPosition;
                float currentAngle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

                if (!isMultiTouch)
                {
                    isMultiTouch = true;
                    initialPinchDistance = currentPinchDist;
                    lastPinchAngle = currentAngle;
                }
                else
                {
                    float deltaDist = currentPinchDist - initialPinchDistance;
                    if (Mathf.Abs(deltaDist) > dragThreshold)
                    {
                        cameraController.Mobile_ZoomCamera(deltaDist * zoomSensitivity);
                        initialPinchDistance = currentPinchDist;
                    }

                    float deltaAngle = Mathf.DeltaAngle(lastPinchAngle, currentAngle);
                    if (Mathf.Abs(deltaAngle) > 0.5f)
                    {
                        cameraController.Mobile_RotateCamera(-deltaAngle * rotationSensitivity);
                        lastPinchAngle = currentAngle;
                    }
                }
                isDragging = true;
            }
        }

        void HandleTap(Vector2 screenPos)
        {
            formationController.Mobile_HandleTap(screenPos);
            UpdateDeselectButtonState();
        }

        void ResetState()
        {
            isDragging = false;
            isMultiTouch = false;
            isLongPressTriggered = false;
            touchStartedOnUI = false;
            if (selectionBoxVisual) selectionBoxVisual.gameObject.SetActive(false);
        }

        void ToggleSelectionMode()
        {
            isSelectionMode = !isSelectionMode;
            UpdateUI();

            if (!isSelectionMode && selectionBoxVisual)
                selectionBoxVisual.gameObject.SetActive(false);
        }

        void DeselectAll()
        {
            formationController.DeselectAllFormationsPublic();
            UpdateDeselectButtonState();
        }

        void UpdateDeselectButtonState()
        {
            if (deselectButton == null || formationController == null) return;
            bool hasSelection = formationController.GetSelectedFormations().Count > 0;
            if (deselectButton.gameObject.activeSelf != hasSelection)
                deselectButton.gameObject.SetActive(hasSelection);
        }

        void UpdateUI()
        {
            if (selectionModeToggleBtn)
            {
                var colors = selectionModeToggleBtn.colors;
                colors.normalColor = isSelectionMode ? selectedColor : Color.white;
                selectionModeToggleBtn.colors = colors;
            }
        }

        void UpdateSelectionBoxUI(Vector2 screenStart, Vector2 screenEnd)
        {
            if (!selectionBoxVisual || !selectionBoxParent) return;

            if (!selectionBoxVisual.gameObject.activeSelf)
                selectionBoxVisual.gameObject.SetActive(true);

            Vector2 min = Vector2.Min(screenStart, screenEnd);
            Vector2 max = Vector2.Max(screenStart, screenEnd);
            Vector2 size = max - min;

            float scaleFactor = rootCanvas.scaleFactor;

            RectTransform rt = selectionBoxVisual.rectTransform;
            rt.anchoredPosition = min / scaleFactor;
            rt.sizeDelta = size / scaleFactor;
        }

        bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;
            if (ETouch.activeTouches.Count > 0)
                return EventSystem.current.IsPointerOverGameObject(ETouch.activeTouches[0].touchId);
            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}