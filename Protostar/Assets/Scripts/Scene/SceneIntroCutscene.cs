using UnityEngine;

public class SceneIntroCutscene : MonoBehaviour
{
    [SerializeField] private ManualCutscene introCutscene;

    private void Start()
    {
        introCutscene.Play();
    }
}
