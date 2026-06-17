using UnityEngine;

// Gan script nay vao bat ky GameObject nao (vi du chinh Button luon cung duoc)
// Keo Canvas/Panel can bat-tat vao field "target"
public class ToggleCanvas : MonoBehaviour
{
    public GameObject target;

    public void Toggle()
    {
        target.SetActive(!target.activeSelf);
    }
}