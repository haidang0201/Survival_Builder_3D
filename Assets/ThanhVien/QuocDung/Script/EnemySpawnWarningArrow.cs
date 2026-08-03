using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class EnemySpawnWarningArrow : MonoBehaviour
{
    [Header("Target & Tracking")]
    public Transform targetEnemy;

    [Header("Mũi Tên Dưới Chân (Ground Arrow)")]
    [Tooltip("Điều chỉnh chiều rộng mũi tên kéo dài bệt dưới chân Enemy")]
    [Range(0.1f, 5f)]
    public float arrowSize = 1.0f;

    [Tooltip("Hệ số điều chỉnh độ dài mũi tên (1.0 = duỗi tới đúng mục tiêu, >1.0 = dài hơn, <1.0 = ngắn hơn)")]
    [Range(0.1f, 5f)]
    public float arrowLengthMultiplier = 1.0f;

    [Tooltip("Độ dài cộng thêm cố định (mét) của mũi tên nếu muốn kéo dài hơn nữa")]
    public float arrowExtraLength = 0.0f;

    [Tooltip("Độ cao của mũi tên sát mặt đất (tránh bị chìm dưới terrain)")]
    [Range(0.01f, 0.5f)]
    public float arrowGroundOffset = 0.05f;

    [Tooltip("Màu sắc của mũi tên dưới chân")]
    public Color arrowColor = new Color(1f, 0.2f, 0.2f, 0.95f);

    [Header("Cảnh Báo Thời Gian (Timer Text)")]
    [Tooltip("Điều chỉnh tỷ lệ kích thước chữ đếm ngược")]
    [Range(0.1f, 5f)]
    public float timerTextScale = 1.0f;
    [Tooltip("Độ cao chữ đếm ngược trên đầu/thân Enemy")]
    [Range(0.5f, 5f)]
    public float textHeightOffset = 1.8f;
    [Tooltip("Màu chữ đếm ngược")]
    public Color textColor = Color.yellow;

    [Header("References (Internal)")]
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private RectTransform arrowRect;
    [SerializeField] private Image arrowImage;
    [SerializeField] private TextMeshProUGUI timerText;

    private Camera mainCamera;

    public static EnemySpawnWarningArrow Create(Transform leadEnemy)
    {
        if (leadEnemy == null) return null;

        EnemySpawnWarningArrow existing = leadEnemy.GetComponentInChildren<EnemySpawnWarningArrow>();
        if (existing != null) return existing;

        GameObject warningObj = new GameObject("EnemySpawnWarning_WorldSpace");
        warningObj.transform.SetParent(leadEnemy, false);
        warningObj.transform.localPosition = Vector3.zero;
        warningObj.transform.localRotation = Quaternion.identity;

        EnemySpawnWarningArrow arrowComp = warningObj.AddComponent<EnemySpawnWarningArrow>();
        arrowComp.targetEnemy = leadEnemy;
        arrowComp.BuildWorldUI();

        return arrowComp;
    }

    private void Awake()
    {
        if (targetEnemy == null && transform.parent != null)
        {
            targetEnemy = transform.parent;
        }
        EnsureComponents();
    }

    private void Start()
    {
        mainCamera = Camera.main;
        EnsureComponents();
        UpdateVisuals();
    }

    private void OnValidate()
    {
        EnsureComponents();
        UpdateVisuals();
    }

    public void BuildWorldUI()
    {
        EnsureComponents();
        UpdateVisuals();
    }

    private void EnsureComponents()
    {
        if (worldCanvas == null)
        {
            worldCanvas = GetComponent<Canvas>();
            if (worldCanvas == null)
            {
                worldCanvas = gameObject.AddComponent<Canvas>();
            }
            worldCanvas.renderMode = RenderMode.WorldSpace;
            
            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;
        }

        // 1. Mũi tên dưới chân bệt mặt đất (Ground Arrow)
        if (arrowRect == null)
        {
            Transform arrowTr = transform.Find("GroundArrow");
            if (arrowTr != null)
            {
                arrowRect = arrowTr as RectTransform;
            }
            else
            {
                GameObject arrowObj = new GameObject("GroundArrow");
                arrowObj.transform.SetParent(transform, false);
                arrowRect = arrowObj.AddComponent<RectTransform>();
            }
        }

        if (arrowRect != null)
        {
            arrowRect.pivot = new Vector2(0.5f, 0f);
            arrowRect.anchorMin = new Vector2(0.5f, 0f);
            arrowRect.anchorMax = new Vector2(0.5f, 0f);
        }

        if (arrowImage == null && arrowRect != null)
        {
            arrowImage = arrowRect.GetComponent<Image>();
            if (arrowImage == null)
            {
                arrowImage = arrowRect.gameObject.AddComponent<Image>();
            }
            arrowImage.sprite = CreateStretchedArrowSprite();
        }

        // 2. Chữ đếm ngược thời gian (Timer Text)
        if (timerText == null)
        {
            Transform textTr = transform.Find("TimerText");
            if (textTr != null)
            {
                timerText = textTr.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                GameObject textObj = new GameObject("TimerText");
                textObj.transform.SetParent(transform, false);
                timerText = textObj.AddComponent<TextMeshProUGUI>();
            }
        }

        if (timerText != null)
        {
            timerText.enableWordWrapping = false;
            timerText.overflowMode = TextOverflowModes.Overflow;
        }
    }

    private Sprite CreateStretchedArrowSprite()
    {
        int width = 64;
        int height = 256;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color transparent = new Color(0, 0, 0, 0);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                tex.SetPixel(x, y, transparent);
            }
        }

        int centerX = width / 2;
        int headHeight = 64;
        int bodyHeight = height - headHeight;

        // 1. Thân mũi tên (Shaft)
        int shaftHalfWidth = 10;
        for (int y = 0; y < bodyHeight; y++)
        {
            for (int x = centerX - shaftHalfWidth; x <= centerX + shaftHalfWidth; x++)
            {
                if (x >= 0 && x < width)
                {
                    tex.SetPixel(x, y, Color.white);
                }
            }
        }

        // 2. Đầu mũi tên tam giác ở đỉnh (Arrowhead)
        for (int y = bodyHeight; y < height; y++)
        {
            float progress = (float)(height - y) / headHeight;
            int rowHalfWidth = Mathf.RoundToInt(progress * (width / 2 - 2));
            for (int x = centerX - rowHalfWidth; x <= centerX + rowHalfWidth; x++)
            {
                if (x >= 0 && x < width)
                {
                    tex.SetPixel(x, y, Color.white);
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.0f));
    }

    public void UpdateVisuals()
    {
        if (arrowRect != null)
        {
            if (arrowImage != null)
            {
                arrowImage.color = arrowColor;
            }
        }

        if (timerText != null)
        {
            float baseScale = 0.015f;
            timerText.fontSize = 32;
            timerText.enableWordWrapping = false;
            timerText.overflowMode = TextOverflowModes.Overflow;
            timerText.color = textColor;
            timerText.alignment = TextAlignmentOptions.Center;
            timerText.rectTransform.sizeDelta = new Vector2(250f, 60f);
            timerText.rectTransform.localScale = Vector3.one * (baseScale * timerTextScale);
            timerText.rectTransform.localPosition = new Vector3(0f, textHeightOffset, 0f);
        }
    }

    private void Update()
    {
        if (targetEnemy == null && transform.parent != null)
        {
            targetEnemy = transform.parent;
        }

        if (Application.isPlaying)
        {
            if (targetEnemy == null || !targetEnemy.gameObject.activeInHierarchy)
            {
                Destroy(gameObject);
                return;
            }

            EnemyAI enemyAI = targetEnemy.GetComponent<EnemyAI>();
            if (enemyAI != null && enemyAI.isCombatActive)
            {
                Destroy(gameObject);
                return;
            }

            UpdateTimerText();
        }
        else
        {
            if (timerText != null)
            {
                timerText.text = "00:45";
            }
        }

        UpdateStretchedArrowGeometry();

        if (timerText != null)
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Vector3 forward = timerText.transform.position - mainCamera.transform.position;
                if (forward.sqrMagnitude > 0.001f)
                {
                    timerText.transform.rotation = Quaternion.LookRotation(forward);
                }
            }
        }

        UpdateVisuals();
    }

    private Transform GetActualEnemyTarget()
    {
        if (targetEnemy == null) return null;

        EnemyAI enemyAI = targetEnemy.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            Transform currentTarget = enemyAI.GetCurrentTarget();
            if (currentTarget != null) return currentTarget;
        }

        float minDist = float.MaxValue;
        Transform bestTarget = null;

        HPTower[] towers = Object.FindObjectsByType<HPTower>(FindObjectsSortMode.None);
        foreach (var t in towers)
        {
            if (t != null && t.gameObject.activeInHierarchy && !t.IsDestroyed && t.CurrentHealth > 0f)
            {
                float dist = Vector3.Distance(targetEnemy.position, t.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    bestTarget = t.transform;
                }
            }
        }

        if (bestTarget != null) return bestTarget;

        UpgradeableBuilding[] buildings = Object.FindObjectsByType<UpgradeableBuilding>(FindObjectsSortMode.None);
        foreach (var b in buildings)
        {
            if (b != null && b.gameObject.activeInHierarchy && !b.IsRuined)
            {
                string bName = b.buildingName.ToLower();
                if (bName.Contains("chính") || bName.Contains("main") || b.CompareTag("Main") || b.gameObject.name.ToLower().Contains("nhachinh"))
                {
                    return b.transform;
                }
            }
        }

        return null;
    }

    private Vector3 GetTargetFeetPosition(Transform target)
    {
        if (target == null) return targetEnemy.position + targetEnemy.forward * 10f;

        Vector3 pos = target.position;

        Collider col = target.GetComponentInChildren<Collider>();
        if (col != null)
        {
            pos = col.bounds.center;
        }
        else
        {
            Renderer ren = target.GetComponentInChildren<Renderer>();
            if (ren != null)
            {
                pos = ren.bounds.center;
            }
        }

        pos.y = targetEnemy.position.y;
        return pos;
    }

    private void UpdateStretchedArrowGeometry()
    {
        if (arrowRect == null || targetEnemy == null) return;

        Transform destinationTarget = GetActualEnemyTarget();

        Vector3 startPos = targetEnemy.position;
        Vector3 endPos = GetTargetFeetPosition(destinationTarget);

        Vector3 dir = endPos - startPos;
        dir.y = 0f;
        float baseDist = dir.magnitude;

        if (baseDist > 0.1f)
        {
            Vector3 forwardDir = dir.normalized;

            arrowRect.pivot = new Vector2(0.5f, 0f);
            arrowRect.position = startPos + Vector3.up * arrowGroundOffset;
            arrowRect.rotation = Quaternion.LookRotation(Vector3.up, forwardDir);

            // BẠN CÓ THỂ ĐIỀU CHỈNH ĐỘ DÀI MŨI TÊN TẠI ĐÂY:
            // finalLength = (Khoảng cách thực tế * arrowLengthMultiplier) + arrowExtraLength
            float finalLength = (baseDist * arrowLengthMultiplier) + arrowExtraLength;

            float widthMeters = 1.2f * arrowSize;
            arrowRect.sizeDelta = new Vector2(widthMeters, finalLength);
            arrowRect.localScale = Vector3.one;
        }
    }

    private void UpdateTimerText()
    {
        if (timerText == null) return;

        float estimatedTime = 0f;

        if (targetEnemy != null)
        {
            Transform destinationTarget = GetActualEnemyTarget();
            Vector3 targetPos = GetTargetFeetPosition(destinationTarget);

            float distance = Vector3.Distance(targetEnemy.position, targetPos);
            EnemyAI enemyAI = targetEnemy.GetComponent<EnemyAI>();
            float stopRange = (enemyAI != null) ? Mathf.Max(enemyAI.CurrentAttackRange, 2.5f) : 2.5f;
            float remainingDist = Mathf.Max(0f, distance - stopRange);

            float speed = (enemyAI != null && enemyAI.chaseSpeed > 0.1f) ? enemyAI.chaseSpeed : 3.5f;
            estimatedTime = remainingDist / speed;
        }

        if (estimatedTime < 0f) estimatedTime = 0f;

        int minutes = Mathf.FloorToInt(estimatedTime / 60f);
        int seconds = Mathf.FloorToInt(estimatedTime % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
