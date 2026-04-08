using UnityEngine;

public class SceneOutroCutscene : MonoBehaviour
{
    [SerializeField] private CutsceneInteractionItem finalItem;
    [SerializeField] private ManualCutscene outroCutscene;

    private void OnEnable()
    {
        if (finalItem == null || outroCutscene == null) return;

        finalItem.Cutscene.OnClose += HandleItemCutsceneClose;
    }

    private void OnDisable()
    {
        if (finalItem == null || outroCutscene == null) return;

        finalItem.Cutscene.OnClose -= HandleItemCutsceneClose;
    }

    private void HandleItemCutsceneClose()
    {
        outroCutscene.Play();
    }
}
