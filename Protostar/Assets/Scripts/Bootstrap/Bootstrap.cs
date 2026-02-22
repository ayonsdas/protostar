using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    [SerializeField] private string initializeScene = "Initialize";
    [SerializeField] private string startScene = "MainMenu";

    private IEnumerator Start()
    {
        // Loads managers in initializeScene
        yield return SceneManager.LoadSceneAsync(initializeScene, LoadSceneMode.Additive);

#if UNITY_EDITOR
        // If in editor, load boot scene then selected scene, otherwise use default startScene
        var start = PlayerPrefs.GetString("BootSceneFirstScene", null);
        if (start != null && start != gameObject.scene.path)
        {
            Debug.Log($"[Bootstrap] requested scene {start} boot scene {gameObject.scene.path}");
            startScene = start;
        }
#endif
        yield return SceneManager.LoadSceneAsync(startScene, LoadSceneMode.Single);

        // Unload bootstrap scene
        SceneManager.UnloadSceneAsync(gameObject.scene);
    }
}