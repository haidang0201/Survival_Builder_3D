namespace TopsonGames.Demo
{
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public class DemoSceneLoader : MonoBehaviour
    {
        public void LoadScene(int index)
        {
            SceneManager.LoadScene(index);
        }
    }

}