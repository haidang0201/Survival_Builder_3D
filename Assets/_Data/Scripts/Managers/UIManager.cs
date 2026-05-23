using UnityEngine;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    public GameObject buildMenu;          // Panel chứa nút xây dựng
    public GameObject warningPanel;       // Panel cảnh báo
    public Text warningText;              // Text thông báo
    public float fadeDuration = 0.5f;     // thời gian fade in/out

    private CanvasGroup warningCanvasGroup;

    [SerializeField] private GameObject houseSelectionPanel;
    [SerializeField] private GameObject workerStatusPanel;

    protected override void Awake()
    {
        base.Awake(); // Singleton logic

        if (warningPanel != null)
        {
            warningCanvasGroup = warningPanel.GetComponent<CanvasGroup>();
            if (warningCanvasGroup == null)
                warningCanvasGroup = warningPanel.AddComponent<CanvasGroup>();

            warningCanvasGroup.alpha = 0f;
            warningPanel.SetActive(false);
        }
    }

    void Start()
    {
        var manager = DayNightManager.Ins;

        manager.OnDayStart += HandleDayStart;
        manager.OnNightStart += HandleNightStart;
    }

    void HandleDayStart()
    {
        if (buildMenu != null)
            buildMenu.SetActive(true);

        if (warningPanel != null)
            StartCoroutine(FadeOutWarning());
    }

    void HandleNightStart()
    {
        if (buildMenu != null)
            buildMenu.SetActive(false);

        if (warningPanel != null && warningText != null)
        {
            warningText.text = "Trời tối, cấm xây dựng!";
            StartCoroutine(FadeInWarning());
        }
    }

    private System.Collections.IEnumerator FadeInWarning()
    {
        warningPanel.SetActive(true);
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            warningCanvasGroup.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        warningCanvasGroup.alpha = 1f;
    }

    private System.Collections.IEnumerator FadeOutWarning()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            warningCanvasGroup.alpha = Mathf.Clamp01(1 - t / fadeDuration);
            yield return null;
        }
        warningCanvasGroup.alpha = 0f;
        warningPanel.SetActive(false);
    }

    public void OnClickHouseButton()
    {
        if (DayNightManager.Ins.IsDay())
            BuildingSystem.Ins.StartPlacing(BuildingType.House);
    }

    public void OnClickForestHutButton()
    {
        if (DayNightManager.Ins.IsDay())
            BuildingSystem.Ins.StartPlacing(BuildingType.ForestHut);
    }

    public void OnClickSawmillButton()
    {
        if (DayNightManager.Ins.IsDay())
            BuildingSystem.Ins.StartPlacing(BuildingType.Sawmill);
    }

    public void OnClickWareHouseButton()
    {
        if (DayNightManager.Ins.IsDay())
            BuildingSystem.Ins.StartPlacing(BuildingType.Warehouse);
    }

    public void OnClickHouseBuilderButton()
    {
        if (DayNightManager.Ins.IsDay())
            BuildingSystem.Ins.StartPlacing(BuildingType.House);
    }
}
