using UnityEngine;
using UnityEngine.SceneManagement; // Bắt buộc phải có để quản lý Scene

public class SceneController : MonoBehaviour
{
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