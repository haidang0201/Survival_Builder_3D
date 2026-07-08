using UnityEngine;
using UnityEngine.SceneManagement; // Bắt buộc phải có để quản lý Scene

public class SceneController : MonoBehaviour
{
    [Header("Settings Configuration")]
    [SerializeField] private GameObject settingsPanel; // Kéo thả Object Setting (Panel) vào đây từ Inspector

    /// <summary>
    /// Hàm chuyển cảnh nhận tên Scene trực tiếp từ sự kiện OnClick UI
    /// </summary>
    /// <param name="sceneName">Tên chính xác của Scene cần chuyển đến</param>
    public void LoadSceneByName(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("Tên Scene truyền vào bị trống! Vui lòng kiểm tra lại trong OnClick.");
        }
    }

    /// <summary>
    /// Hàm CHUYÊN DÙNG ĐỂ MỞ Setting Panel (Gán cho nút Setting ở Menu chính)
    /// </summary>
    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("Chưa gán Object Settings vào SceneController trong Inspector!");
        }
    }

    /// <summary>
    /// Hàm CHUYÊN DÙNG ĐỂ ĐÓNG Setting Panel (Gán cho nút X hoặc nút Back bên trong Panel)
    /// </summary>
    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("Chưa gán Object Settings vào SceneController trong Inspector!");
        }
    }

    /// <summary>
    /// Hàm thoát game hoàn toàn
    /// </summary>
    public void ExitGame()
    {
        Debug.Log("Đã bấm nút Thoát Game!");
        
        // Thoát ứng dụng khi đã build thành phẩm (PC/Mobile)
        Application.Quit();

        #if UNITY_EDITOR
        // Dừng chế độ Playmode nếu đang test trong Unity Editor
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}