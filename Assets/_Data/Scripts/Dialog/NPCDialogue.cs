using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NPCDialogue : MonoBehaviour
{
    public GameObject panel;
    public TextMeshProUGUI text;
    public Button continueButton;

    public float typeSpeed = 0.03f;

    bool isTypingDone;
    bool continueClicked;
    string currentMsg;

    Coroutine typingRoutine;

    void Awake()
    {
        panel.SetActive(false);
        continueButton.onClick.AddListener(OnContinue);
        continueButton.gameObject.SetActive(false);
    }

    public void Show(string msg)
    {
        currentMsg = msg;
        isTypingDone = false;
        continueClicked = false;

        panel.SetActive(true);

        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        typingRoutine = StartCoroutine(Type(msg));
    }

    public IEnumerator ShowAndWait(string msg)
    {
        Show(msg);

        yield return new WaitUntil(() => isTypingDone);

        continueButton.gameObject.SetActive(true);

        yield return new WaitUntil(() => continueClicked);

        continueButton.gameObject.SetActive(false);
    }

    IEnumerator Type(string msg)
    {
        text.text = "";

        foreach (char c in msg)
        {
            text.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        isTypingDone = true;
    }

    void OnContinue()
    {
        continueClicked = true;
    }
}