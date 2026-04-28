using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerModeSwitcher : MonoBehaviour
{
    public PlayerController movement;
    public PlayerCamera playerCamera;
    public Camera playerCamObj;

    public BuildCameraController buildCamera;

    private bool isBuildMode = false;

    void Start()
    {
        SetPlayerMode();
    }

    void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            isBuildMode = !isBuildMode;

            if (isBuildMode)
                SetBuildMode();
            else
                SetPlayerMode();
        }
    }

    void SetPlayerMode()
    {
        movement.SetMove(true);
        playerCamera.SetLook(true);

        playerCamObj.gameObject.SetActive(true);
        buildCamera.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
    }

    void SetBuildMode()
    {
        movement.SetMove(false);
        playerCamera.SetLook(false);

        playerCamObj.gameObject.SetActive(false);
        buildCamera.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
    }
}