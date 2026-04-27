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
        AudioManager.Instance.SetMusicParameter("IntroComplete", 1);
    }
}
