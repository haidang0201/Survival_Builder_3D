using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Transform _mainCameraTransform;

    void Start()
    {
        if (Camera.main != null)
        {
            _mainCameraTransform = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (_mainCameraTransform != null)
        {
            // Ép UI luôn quay mặt về hướng camera
            transform.LookAt(transform.position + _mainCameraTransform.forward);
        }
    }
}