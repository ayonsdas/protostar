using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    [SerializeField] private string initializeScene = "Initialize";
    [SerializeField] private string startScene = "MainMenu";

    void Awake()
    {
        // Loads managers in initializeScene
        SceneManager.LoadSceneAsync(initializeScene, LoadSceneMode.Additive);
    }

    void Start()
    {
        
#if UNITY_EDITOR
        // If in editor, load boot scene then selected scene, otherwise use default startScene
        var start = PlayerPrefs.GetString("BootSceneFirstScene", null);
        if (start != null)
        {
            startScene = start;
        }
#endif
        SceneManager.LoadSceneAsync(startScene, LoadSceneMode.Single);
    }
}