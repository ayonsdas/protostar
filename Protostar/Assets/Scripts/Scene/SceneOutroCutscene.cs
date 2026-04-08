using UnityEngine;

public class SceneOutroCutscene : MonoBehaviour
{
    [SerializeField] private CutsceneInteractionItem finalItem;
    [SerializeField] private ManualCutscene outroCutscene;
    [SerializeField] private bool returnToMenu = true;

    private void OnEnable()
    {
        if (finalItem == null || outroCutscene == null) return;

        finalItem.Cutscene.OnClose += HandleItemCutsceneClose;
        outroCutscene.OnClose += HandleOutroCutsceneClose;
    }

    private void OnDisable()
    {
        if (finalItem == null || outroCutscene == null) return;

        finalItem.Cutscene.OnClose -= HandleItemCutsceneClose;
        outroCutscene.OnClose -= HandleOutroCutsceneClose;
    }

    private void HandleItemCutsceneClose()
    {
        outroCutscene.Play();
    }

    private void HandleOutroCutsceneClose()
    {
        if (returnToMenu)
        {
            GameStateManager.Instance.ReturnToMainMenu();
        }
    }
}
