using UnityEngine;

public class WorkerModel : MonoBehaviour
{
    // Ẩn hoặc hiển thị mô hình công nhân
    public void SetActive(bool isActive)
    {
        gameObject.SetActive(isActive); // Ẩn hoặc hiển thị GameObject của công nhân
    }
}