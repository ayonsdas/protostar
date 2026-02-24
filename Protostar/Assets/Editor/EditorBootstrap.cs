#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class EditorInit
{
    private static SceneAsset bootScene;
    static EditorInit()
    {
        // Select boot scene to be loaded to
        bootScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Boot.unity");
        if (bootScene == null)
        {
            Debug.LogError("[EditorBootstrap] Cannot find boot scene");
        }
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            PlayerPrefs.SetString("BootSceneFirstScene", activeScene.path);

            EditorSceneManager.playModeStartScene = bootScene;
        }
    }
}
#endif