using UnityEngine;

public class SceneIntroCutscene : MonoBehaviour
{
    [SerializeField] private ImageCutscene introCutscene;

    private void Start()
    {
        if (MenuManager.Instance == null) return;

        MenuManager.Instance.AddCutsceneCloseCallback(introCutscene, HandleIntroClose);
        MenuManager.Instance.PlayCutscene(introCutscene);
    }

    private void HandleIntroClose()
    {
        try
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicParameter("IntroComplete", 1);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SceneIntroCutscene] Failed to set music parameter: {e.Message}");
        }
    }
}
