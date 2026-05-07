using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSwitcher : MonoBehaviour
{
    public Camera playerCamera;
    public Camera buildCamera;

    private bool isPlayerView = true;

    void Start()
    {
        playerCamera.enabled = true;
        buildCamera.enabled = false;
    }

    void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            SwitchCamera();
        }
    }

    void SwitchCamera()
    {
        isPlayerView = !isPlayerView;

        playerCamera.enabled = isPlayerView;
        buildCamera.enabled = !isPlayerView;
    }
}