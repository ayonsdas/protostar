using System;
using FMODUnity;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class ImageCutscenePresenter : MonoBehaviour, IMenuView, ICutscenePlayer<ImageCutscene>
{
    [Header("Button SFX")]
    [SerializeField] private EventReference clickSound;
    [SerializeField] private EventReference hoverSound;

    private ImageCutscene cutscene;

    private int currentPage = 0;

    private UIDocument document;
    private VisualElement root;
    private VisualElement _background;
    private Button _backButton;
    private Button _nextButton;

    public event Action OnClose;

    private void OnEnable()
    {
        if (MenuManager.Instance)
        {
            MenuManager.Instance.RegisterCutscenePlayer(this);
        }
        else
        {
            Debug.LogWarning("[ImageCutscenePresenter] cannot find MenuManger to register with");
        }
    }

    private void OnDisable()
    {
        if (MenuManager.Instance != null)
            MenuManager.Instance.UnregisterCutscenePlayer(this);
    }

    void Awake()
    {
        document = GetComponent<UIDocument>();
        root = document.rootVisualElement;

        _background = root.Q("background-image");
        _backButton = root.Q<Button>("back-button");
        _nextButton = root.Q<Button>("next-button");

        UIHelper.RegisterButton(_backButton, HandleBackButtonClicked, clickSound: clickSound, hoverSound: hoverSound);
        UIHelper.RegisterButton(_nextButton, HandleNextButtonClicked, clickSound: clickSound, hoverSound: hoverSound);
    }

    private void HandleNextButtonClicked()
    {
        if (currentPage + 1 >= cutscene.Length)
        {
            // Exits out of cutscene state to previous
            GameStateManager.Instance.RevertState();
            document.rootVisualElement.style.display = DisplayStyle.None;
            currentPage = 0;

            OnClose?.Invoke();
            return;
        }

        currentPage++;
        UpdateUI();
    }

    private void HandleBackButtonClicked()
    {
        currentPage--;
        UpdateUI();
    }

    private void UpdateUI()
    {
        bool isLastPage = currentPage + 1 == cutscene.Length;
        _nextButton.text = isLastPage ? "Close" : "Next";
        _background.style.backgroundImage = new StyleBackground(cutscene.Frames[currentPage]);
        _backButton.style.display = currentPage > 0 ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void OnGameStateChanged(GameState state)
    {
        bool active = state == GameState.Cutscene;
        root.Display(active);
        if (active)
        {
            _nextButton?.Focus();
        }
    }

    public void OnInputModeChanged(InputMode inputMode)
    {
        switch (inputMode)
        {
            case InputMode.Mouse:
                root.focusController.focusedElement?.Blur();
                UIHelper.SetMouseMode(root);
                break;
            case InputMode.Controller:
                UIHelper.SetControllerMode(root);
                _nextButton?.Focus();
                break;
        }
    }

    public void Play(ImageCutscene cutscene)
    {
        this.cutscene = cutscene;
        currentPage = 0;
        UpdateUI();
        GameStateManager.Instance.SetState(GameState.Cutscene);
    }
}
