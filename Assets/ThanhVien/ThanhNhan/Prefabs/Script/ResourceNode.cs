using UnityEngine;

public class ResourceNode : MonoBehaviour
{
    [Header("Lock System")]
    public GameObject lockCanvas; // Ô để kéo thả Lock_Canvas vào
    public bool isLocked = true;   // Trạng thái khóa ban đầu

    [Header("UI Panel Ref")]
    public GameObject moDaCardPanel; // Kéo thả cái bảng trắng MoDaCard vào đây

    private StoneMineUnlockPanelController panelController;

    void Start()
    {
        // Ban đầu cập nhật trạng thái ổ khóa lơ lửng trên đầu
        UpdateLockStatus();
        
        // Đảm bảo bảng thông tin MoDaCard luôn ẩn lúc vào game
        if (moDaCardPanel != null)
        {
            moDaCardPanel.SetActive(false);
            panelController = moDaCardPanel.GetComponent<StoneMineUnlockPanelController>();
        }
    }

    public void UpdateLockStatus()
    {
        if (lockCanvas != null)
        {
            // Nếu isLocked = true thì hiện ổ khóa, false thì ẩn ổ khóa
            lockCanvas.SetActive(isLocked); 
        }
    }

    // Hàm này tự động chạy khi người chơi click chuột vào Collider của mỏ đá
    void OnMouseDown()
    {
        if (isLocked)
        {
            Debug.Log("Mỏ đá đang bị khóa! Bật bảng UI nhiệm vụ lên.");
            if (moDaCardPanel != null)
            {
                if (panelController == null)
                {
                    panelController = moDaCardPanel.GetComponent<StoneMineUnlockPanelController>();
                }

                if (panelController != null)
                {
                    panelController.BindTargetNode(this);
                    panelController.RefreshPanelData();
                }

                moDaCardPanel.SetActive(true); // Bật bảng trắng lên
            }
        }
        else
        {
            Debug.Log("Mỏ đá đã mở khóa! Tiến hành khai thác...");
            // Viết logic cộng đá hoặc cho worker làm việc ở đây
        }
    }

    public void UnlockNode()
    {
        isLocked = false;
        UpdateLockStatus();
        Debug.Log("Mỏ đá đã được mở khóa thành công.");
    }
    // Thêm hàm này vào dưới cùng script ResourceNode của bạn
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log("Chuột đang click trúng vào Object: " + hit.transform.name);
            }
            else
            {
                Debug.Log("Cú click chuột bay ra ngoài không trung, không trúng Collider nào cả!");
            }
        }
    }
}