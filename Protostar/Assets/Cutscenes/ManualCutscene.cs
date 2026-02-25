using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class ManualCutscene : MonoBehaviour
{
    public Image cutsceneImage;
    public Sprite[] cutsceneFrames;
    public Button backButton;
    public Button nextButton;
    public TMP_Text nextButtonText;

    public int currentPage = 0;

    void Start()
    {
        // Lock player movement when cutscene starts
        GameStateManager.Instance.SetState(GameState.Cutscene);

        backButton.gameObject.SetActive(false);
        cutsceneImage.sprite = cutsceneFrames[0];
    }

    public void NextButton()
    {
        if (currentPage + 1 == cutsceneFrames.Length)
        {
            // Unlock player movement when video ends
            GameStateManager.Instance.RevertState();
            gameObject.SetActive(false);
            return;
        }

        currentPage++;
        UpdateUI();
    }

    public void BackButton()
    {
        currentPage--;
        UpdateUI();
    }

    public void UpdateUI()
    {
        bool isLastPage = currentPage + 1 == cutsceneFrames.Length;
        nextButtonText.text = isLastPage ? "End" : "Next";

        cutsceneImage.sprite = cutsceneFrames[currentPage];
        backButton.gameObject.SetActive(currentPage > 0);
    }
}
