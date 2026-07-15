using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIWarning : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button warningButton;

    [Header("Floating Animation Settings")]
    [SerializeField] private float floatSpeed = 3f;
    [SerializeField] private float floatAmplitude = 0.25f;
    [SerializeField] private Vector3 worldScale = new Vector3(0.008f, 0.008f, 0.008f);

    private Vector3 spawnPosition;
    private Vector3 canvasWorldPosition;
    private Vector3 startPosition;
    private bool isClicked = false;
    private Transform mainCameraTransform;
    private Canvas parentCanvas;

    public void Initialize(Vector3 groundPosition, Vector3 canvasPosition)
    {
        spawnPosition = groundPosition;
        canvasWorldPosition = canvasPosition;
    }

    private void Start()
    {
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }

        if (warningButton == null)
        {
            warningButton = GetComponentInChildren<Button>();
        }

        if (warningButton != null)
        {
            warningButton.onClick.AddListener(OnWarningClicked);
        }

        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            parentCanvas.renderMode = RenderMode.WorldSpace;
            parentCanvas.worldCamera = Camera.main;
            parentCanvas.transform.localScale = worldScale;

            // Set size of the Canvas RectTransform so it has a bounding volume and is not culled
            RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                canvasRect.sizeDelta = new Vector2(300f, 200f);
            }

            // Restore the correct world position passed from EnemySpawn
            if (canvasWorldPosition != Vector3.zero)
            {
                transform.position = canvasWorldPosition;
            }

            // Center the button in the canvas to avoid offsets from layout dragging in editor
            if (warningButton != null)
            {
                RectTransform buttonRect = warningButton.GetComponent<RectTransform>();
                if (buttonRect != null)
                {
                    buttonRect.anchoredPosition = Vector2.zero;
                }
            }
        }

        // Fallback: If spawnPosition was not set via Initialize, use current position
        if (spawnPosition == Vector3.zero)
        {
            spawnPosition = transform.position;
        }

        startPosition = transform.position;
    }

    private void Update()
    {
        if (isClicked) return;

        // Floating animation (Micro-animation)
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void LateUpdate()
    {
        // Billboard rotation so it always faces camera, only if it is a World Space Canvas
        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace && mainCameraTransform != null)
        {
            transform.rotation = mainCameraTransform.rotation;
        }
    }

    public void OnWarningClicked()
    {
        if (isClicked) return;
        isClicked = true;

        if (warningButton != null)
        {
            warningButton.interactable = false; // Prevent double click
        }

        Debug.Log($"[UIWarning] Warning button clicked at {spawnPosition}. Commanding all soldiers to attack!");

        // Find all UnitControllers on the map
        UnitController[] soldiers = FindObjectsOfType<UnitController>();
        int count = 0;
        foreach (UnitController soldier in soldiers)
        {
            if (soldier != null)
            {
                soldier.RespondToWarning(spawnPosition);
                count++;
            }
        }

        Debug.Log($"[UIWarning] Notified {count} soldiers.");

        // Visual feedback animation (Scale down and destroy)
        StartCoroutine(ScaleDownAndDestroyRoutine());
    }

    private IEnumerator ScaleDownAndDestroyRoutine()
    {
        Vector3 initialScale = transform.localScale;
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, elapsed / duration);
            yield return null;
        }

        Destroy(gameObject);
    }

    public Vector3 GetSpawnPosition()
    {
        return spawnPosition;
    }
}

