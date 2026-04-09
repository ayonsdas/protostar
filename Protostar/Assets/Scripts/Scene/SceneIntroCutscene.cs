using UnityEngine;

public class SceneIntroCutscene : MonoBehaviour
{
    [SerializeField] private ImageCutscene introCutscene;

    private void Start()
    {
        if (MenuManager.Instance == null) return;

        MenuManager.Instance.PlayCutscene(introCutscene);
    }
}
