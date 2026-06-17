using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroManager : MonoBehaviour
{
    [Header("Cài đặt Intro")]
    public float delayBeforeHandbook = 0.5f; // chờ chút rồi mở
    public string gameplaySceneName = "GameplayScene";

    void Start()
    {
        // Mở bảng hướng dẫn sau khi scene load xong
        Invoke(nameof(OpenHandbook), delayBeforeHandbook);
    }

    void OpenHandbook()
    {
        HandbookController.Instance.Show();
    }

    // Gọi hàm này từ nút "Bắt đầu chơi" trong HandbookPanel
    public void OnStartGameClicked()
    {
        HandbookController.Instance.Hide();
        Invoke(nameof(LoadGameplay), 0.4f); // chờ fade out xong
    }

    void LoadGameplay()
    {
        SceneManager.LoadScene(gameplaySceneName);
    }
}