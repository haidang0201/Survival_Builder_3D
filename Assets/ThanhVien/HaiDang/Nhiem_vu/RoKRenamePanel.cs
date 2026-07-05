using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class RoKRenamePanel : MonoBehaviour
{
    [Header("ROOT")]
    public GameObject profilePanelRoot;
    public GameObject renamePanelRoot;

    [Header("BUTTONS")]
    public Button profileButton;
    public Button closeProfileButton;
    public Button renameButton;
    public Button confirmButton;
    public Button cancelButton;

    [Header("TEXT / INPUT")]
    public TMP_Text currentNameText;
    public TMP_Text warningText;
    public TMP_InputField nameInputField;

    [Header("QUEST LINK")]
    public RoKQuestPanelUI questPanel;
    public string renameQuestId = "my_name";

    [Header("SETTINGS")]
    public string defaultName = "Thống đốc";
    public int minLength = 2;
    public int maxLength = 16;
    public string playerPrefsKey = "PLAYER_NAME";

    [Header("CANVAS LAYER")]
    public bool forceTopCanvas = true;
    public int sortingOrder = 7000;

    [Header("EVENT")]
    public UnityEvent onRenameConfirmed;
    public UnityEvent<string> onNameConfirmed;

    string currentName;

    void Awake()
    {
        BindButtons();
        ApplyCanvasLayer();
        LoadName();

        if (profilePanelRoot != null)
            profilePanelRoot.SetActive(false);

        if (renamePanelRoot != null)
            renamePanelRoot.SetActive(false);
    }

    void BindButtons()
    {
        if (profileButton != null)
        {
            profileButton.onClick.RemoveListener(OpenProfile);
            profileButton.onClick.AddListener(OpenProfile);
        }

        if (closeProfileButton != null)
        {
            closeProfileButton.onClick.RemoveListener(CloseProfile);
            closeProfileButton.onClick.AddListener(CloseProfile);
        }

        if (renameButton != null)
        {
            renameButton.onClick.RemoveListener(OpenRenamePanel);
            renameButton.onClick.AddListener(OpenRenamePanel);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(ConfirmRename);
            confirmButton.onClick.AddListener(ConfirmRename);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(CloseRenamePanel);
            cancelButton.onClick.AddListener(CloseRenamePanel);
        }
    }

    public void OpenProfile()
    {
        ApplyCanvasLayer();

        if (profilePanelRoot != null)
            profilePanelRoot.SetActive(true);

        RefreshNameUI();
    }

    public void CloseProfile()
    {
        if (renamePanelRoot != null)
            renamePanelRoot.SetActive(false);

        if (profilePanelRoot != null)
            profilePanelRoot.SetActive(false);
    }

    public void OpenRenamePanel()
    {
        ApplyCanvasLayer();

        if (renamePanelRoot != null)
            renamePanelRoot.SetActive(true);

        if (warningText != null)
            warningText.text = "";

        if (nameInputField != null)
        {
            nameInputField.text = currentName;
            nameInputField.Select();
            nameInputField.ActivateInputField();
        }
    }

    public void CloseRenamePanel()
    {
        if (renamePanelRoot != null)
            renamePanelRoot.SetActive(false);
    }

    public void ConfirmRename()
    {
        if (nameInputField == null)
            return;

        string newName = nameInputField.text.Trim();

        if (newName.Length < minLength)
        {
            ShowWarning("Tên quá ngắn.");
            return;
        }

        if (newName.Length > maxLength)
        {
            ShowWarning("Tên quá dài.");
            return;
        }

        currentName = newName;

        PlayerPrefs.SetString(playerPrefsKey, currentName);
        PlayerPrefs.Save();

        RefreshNameUI();
        CloseRenamePanel();

        if (questPanel != null)
            questPanel.CompleteQuest(renameQuestId);

        onNameConfirmed?.Invoke(currentName);
        onRenameConfirmed?.Invoke();

        Debug.Log("[RoKRenamePanel] Đổi tên thành: " + currentName);
    }

    void LoadName()
    {
        currentName = PlayerPrefs.GetString(playerPrefsKey, defaultName);
        RefreshNameUI();
    }

    void RefreshNameUI()
    {
        if (currentNameText != null)
            currentNameText.text = currentName;
    }

    void ShowWarning(string msg)
    {
        if (warningText != null)
            warningText.text = msg;
    }

    void ApplyCanvasLayer()
    {
        if (!forceTopCanvas)
            return;

        if (profilePanelRoot == null)
            return;

        profilePanelRoot.transform.SetAsLastSibling();

        Canvas c = profilePanelRoot.GetComponent<Canvas>();
        if (c == null)
            c = profilePanelRoot.AddComponent<Canvas>();

        c.overrideSorting = true;
        c.sortingOrder = sortingOrder;

        if (profilePanelRoot.GetComponent<GraphicRaycaster>() == null)
            profilePanelRoot.AddComponent<GraphicRaycaster>();
    }
}