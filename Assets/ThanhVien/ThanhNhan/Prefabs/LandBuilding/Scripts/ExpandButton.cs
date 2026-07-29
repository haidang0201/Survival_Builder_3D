using UnityEngine;
using UnityEngine.EventSystems;

// Sử dụng giao diện IPointerClickHandler để nhận event click chuẩn từ UI Canvas
public class ExpandButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private ExpandDirection direction;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (LandGridManager.Ins != null)
        {
            LandGridManager.Ins.ExpandGrid(direction);
        }
    }
}