using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerModeSwitcher : MonoBehaviour
{
    public PlayerController movement;
    public PlayerCamera playerCamera;
    public Camera playerCamObj;

    public BuildCameraController buildCamera;

    [Header("Input")]
    public Key switchModeKey = Key.Tab;

    private bool isBuildMode = false;

    void Start()
    {
        SetPlayerMode();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard[switchModeKey].wasPressedThisFrame)
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
        if (movement != null)
            movement.SetMove(true);

        if (playerCamera != null)
        {
            playerCamera.enabled = true;
            playerCamera.SetLook(true);
        }

        if (playerCamObj != null)
            playerCamObj.gameObject.SetActive(true);

        if (buildCamera != null)
            buildCamera.SetActive(false);

        // reset cursor cho player mode
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void SetBuildMode()
    {
        if (movement != null)
            movement.SetMove(false);

        if (playerCamera != null)
        {
            playerCamera.SetLook(false);
            playerCamera.enabled = false;
        }

        if (playerCamObj != null)
            playerCamObj.gameObject.SetActive(false);

        if (buildCamera != null)
            buildCamera.SetActive(true);

        // ép lại cursor ngay khi vào build mode
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}