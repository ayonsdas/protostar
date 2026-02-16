using System;
using UnityEngine;
using UnityEngine.UIElements;

public class CreditsPresenter
{
    private Button returnButton;
    public Action BackAction { get; set; }

    public CreditsPresenter(VisualElement root)
    {
        returnButton = root.Q<Button>("Return");
        returnButton.clicked += () => BackAction?.Invoke();
        returnButton.RegisterCallback<NavigationSubmitEvent>(_ => BackAction?.Invoke());
    }
}