using System.Collections;
using UnityEngine;

public class ResourceTree : MonoBehaviour
{
    // Định nghĩa 4 cấp độ đúng chuẩn theo yêu cầu của bạn
    public enum TreeStage { Small = 0, Medium = 1, Large = 2, Stump = 3 }

    [Header("Trạng thái hiện tại")]
    public TreeStage currentStage = TreeStage.Large; // Mặc định vào game là Cây Lớn

    [Header("Thời gian lớn lên (giây)")]
    public float growthInterval = 5f;

    [Header("ĐỒNG HỒ ĐẾM NGƯỢC (Xem trong Play Mode)")]
    [SerializeField] private float timeRemaining = 0f; // Biến này sẽ nhảy lùi số công khai trên Inspector

    [Header("Mô hình hiển thị (Kéo thả đúng thứ tự này)")]
    [Tooltip("0: Cây nhỏ, 1: Cây vừa, 2: Cây lớn, 3: Gốc cây")]
    public GameObject[] stageVisuals;

    private Coroutine growthCoroutine;

    void Start()
    {
        UpdateTreeVisuals();

        // Nếu ban đầu cây chưa phải Cây Lớn, kích hoạt tự động mọc
        if (currentStage != TreeStage.Large)
        {
            growthCoroutine = StartCoroutine(GrowthRoutine());
        }
    }

    // Hàm xử lý khi Worker đến chặt cây
    public void BeChopped()
    {
        if (currentStage == TreeStage.Stump) return; // Đang là gốc thì không cho chặt tiếp

        // 1. Thu hoạch gỗ dựa trên cấp độ cây hiện tại
        int woodReward = GetWoodAmount(currentStage);
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.Wood += woodReward;
        }
        Debug.Log($"[TREE_SYSTEM] Đã chặt! Nhận {woodReward} gỗ từ [{currentStage}].");

        // 2. Chuyển ngay lập tức về trạng thái GỐC CÂY
        currentStage = TreeStage.Stump;
        UpdateTreeVisuals();

        // 3. Kích hoạt chu kỳ mọc lại và reset đồng hồ đếm ngược
        if (growthCoroutine != null) StopCoroutine(growthCoroutine);
        growthCoroutine = StartCoroutine(GrowthRoutine());
    }

    // Logic mọc cây tuần tự và chạy đồng hồ đếm ngược từng giây
    private IEnumerator GrowthRoutine()
    {
        while (currentStage != TreeStage.Large)
        {
            // Reset đồng hồ về mức max (ví dụ: 5 giây) trước khi đếm ngược cho cấp này
            timeRemaining = growthInterval;

            // Vòng lặp đếm ngược mượt mà theo thời gian thực của game
            while (timeRemaining > 0f)
            {
                timeRemaining -= Time.deltaTime; // Trừ dần thời gian qua mỗi khung hình
                yield return null; // Chờ sang khung hình tiếp theo để cập nhật số lên Inspector
            }

            timeRemaining = 0f; // Khóa về vị trí 0 khi hết giờ

            // Hết 5 giây -> Tiến hành chuyển cấp tuần tự
            if (currentStage == TreeStage.Stump)
            {
                currentStage = TreeStage.Small; // Gốc mọc thành Cây nhỏ
            }
            else if (currentStage == TreeStage.Small)
            {
                currentStage = TreeStage.Medium; // Cây nhỏ lên Cây vừa
            }
            else if (currentStage == TreeStage.Medium)
            {
                currentStage = TreeStage.Large; // Cây vừa lên Cây lớn
            }

            UpdateTreeVisuals();
            Debug.Log($"[TREE_SYSTEM] Đã hết 5 giây đếm ngược! Cây lớn lên cấp: {currentStage}");
        }

        timeRemaining = 0f; // Cây đạt cấp tối đa thì đồng hồ về 0 hẳn
        growthCoroutine = null;
    }

    // Cập nhật bật/tắt các Model 3D tương ứng trong Unity
    private void UpdateTreeVisuals()
    {
        for (int i = 0; i < stageVisuals.Length; i++)
        {
            if (stageVisuals[i] != null)
            {
                stageVisuals[i].SetActive(i == (int)currentStage);
            }
        }
    }

    // Trả về lượng tài nguyên thu hoạch
    private int GetWoodAmount(TreeStage stage)
    {
        return stage switch
        {
            TreeStage.Small => 15,
            TreeStage.Medium => 30,
            TreeStage.Large => 60,
            _ => 0
        };
    }
}