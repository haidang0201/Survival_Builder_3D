// using UnityEngine;
// using UnityEngine.UI;

// public class BuildingHealthBarUI : MonoBehaviour
// {
//     [Header("Cấu Hình Thành Phần")]
//     [SerializeField] private Slider hpSlider;
//     [SerializeField] private GameObject visualGroup; // Kéo cụm UI Panel chứa Slider để ẩn/hiện linh hoạt

//     private HPTower targetHP;

//     private void Awake()
//     {
//         // Tự động tìm thành phần quản lý máu HPTower nằm trên công trình cha
//         targetHP = GetComponentInParent<HPTower>();
//         if (hpSlider == null) hpSlider = GetComponentInChildren<Slider>();
//     }

//     private void OnEnable()
//     {
//         if (targetHP != null)
//         {
//             // Đăng ký nhận thông tin mỗi khi HPTower bị trừ máu
//             targetHP.OnHPChanged += UpdateHPBar;

//             // Cập nhật trạng thái hiển thị ban đầu
//             UpdateHPBar(targetHP.CurrentHealth, targetHP.MaxHealth);
//         }
//     }

//     private void OnDisable()
//     {
//         if (targetHP != null)
//         {
//             targetHP.OnHPChanged -= UpdateHPBar;
//         }
//     }

//     private void UpdateHPBar(float currentHP, float maxHP)
//     {
//         if (hpSlider != null)
//         {
//             hpSlider.maxValue = maxHP;
//             hpSlider.value = currentHP;
//         }

//         if (visualGroup != null)
//         {
//             // CHUẨN UX: Đầy máu (100%) hoặc đã sập hẳn (0%) thì ẩn thanh máu đi cho sạch màn hình.
//             // Chỉ hiện thanh máu khi đang thực sự bị quái đánh mất máu (0 < HP < Max)
//             bool shouldShow = currentHP < maxHP && currentHP > 0;
//             visualGroup.SetActive(shouldShow);
//         }
//     }

//     private void LateUpdate()
//     {
//         // Luôn giữ thanh máu hướng thẳng về phía Camera (Billboarding) không bị méo góc khi xoay Map
//         if (Camera.main != null && visualGroup != null && visualGroup.activeSelf)
//         {
//             transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
//         }
//     }
// }