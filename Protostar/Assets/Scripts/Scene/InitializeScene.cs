using UnityEngine;
using UnityEngine.SceneManagement;

public class InitializeScene : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    void Start()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
