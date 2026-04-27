using UnityEngine;

public class SceneOutroCutscene : MonoBehaviour
{
    [SerializeField] private CutsceneInteractionItem finalItem;
    [SerializeField] private ImageCutscene outroCutscene;

    private void Start()
    {
        if (MenuManager.Instance == null) return;

        MenuManager.Instance.AddCutsceneCloseCallback(finalItem.Cutscene, HandleItemCutsceneClose);
    }

    private void HandleItemCutsceneClose()
    {
        if (MenuManager.Instance == null) return;
        Debug.Log("[SceneOutroCutscene] Item cutscene closed, playing outro");

        MenuManager.Instance.AddCutsceneCloseCallback(outroCutscene, HandleOutroCutsceneClose);
        MenuManager.Instance.PlayCutscene(outroCutscene);

        if (AudioManager.Instance == null) return;
        AudioManager.Instance.SetMusicParameter("GameComplete", 1);
    }

    private void HandleOutroCutsceneClose()
    {
        Debug.Log("[SceneOutroCutscene] Outro cutscene closed, returning to menu");
        if (GameStateManager.Instance == null) return;

        GameStateManager.Instance.ReturnToMainMenu();
    }
}
