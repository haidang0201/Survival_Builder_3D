using UnityEngine;
using UnityEngine.UI;

public class HandbookButton : MonoBehaviour
{
    private Button btn;

    void Start()
    {
        btn = GetComponent<Button>();
        btn.onClick.AddListener(OnClick);
    }

    void Update()
    {
        // Phím tắt H để mở/đóng
        if (Input.GetKeyDown(KeyCode.H))
            OnClick();
    }

    void OnClick()
    {
        HandbookController.Instance.Toggle();
    }
}