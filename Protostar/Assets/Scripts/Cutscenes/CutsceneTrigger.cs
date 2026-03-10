using UnityEngine;

public class CutsceneTrigger : MonoBehaviour
{
    private bool cutsceneTriggered = false;
    public ManualCutscene manualCutscene;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player") && cutsceneTriggered == false)
        {
            cutsceneTriggered = true;
            manualCutscene.cutsceneCanvas.enabled = true;
            // Lock player movement when cutscene starts
            GameStateManager.Instance.SetState(GameState.Cutscene);
        }
    }
}
