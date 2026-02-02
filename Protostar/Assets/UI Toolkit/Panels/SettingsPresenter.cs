using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SettingsPresenter
{
    private const string RESOLUTION_KEY = "Settings_Resolution";
    private const string FULLSCREEN_KEY = "Settings_Fullscreen";
    private const string MUSIC_MASTER_KEY = "Settings_MusicMaster";

    private List<string> resolutions = new List<string>()
    {
        "3840x2160",
        "2560x1440",
        "1920x1080",
        "1600x900",
        "1280x720"
    };

    public Action BackAction { set { if (backButton != null) backButton.clicked += value; } }
    public Action ReturnToMainMenuAction { set { if (mainMenuButton != null) mainMenuButton.clicked += value; } }

    private Button backButton;
    private Button mainMenuButton;
    private Toggle fullscreenToggle;
    private DropdownField resolutionsDropdown;
    private Slider musicMasterSlider;


    public SettingsPresenter(VisualElement root)
    {
        if (root == null)
        {
            Debug.LogError("Settings root is null");
            return;
        }

        backButton = root.Q<Button>("BackButton");
        mainMenuButton = root.Q<Button>("MainMenuButton");
        fullscreenToggle = root.Q<Toggle>("FullscreenToggle");
        resolutionsDropdown = root.Q<DropdownField>("ResolutionDropdown");
        musicMasterSlider = root.Q<Slider>("MusicMasterSlider");

        if (backButton == null)
        {
            Debug.LogError("BackButton not found in Settings");
        }

        if (fullscreenToggle != null)
        {
            // Load saved fullscreen setting
            fullscreenToggle.value = PlayerPrefs.GetInt(FULLSCREEN_KEY, Screen.fullScreen ? 1 : 0) == 1;
            fullscreenToggle.RegisterCallback<MouseUpEvent>((evt) => { SetFullscreen(fullscreenToggle.value); }, TrickleDown.TrickleDown);
        }

        if (resolutionsDropdown != null)
        {
            resolutionsDropdown.choices = resolutions;

            // Load saved resolution index
            int savedIndex = PlayerPrefs.GetInt(RESOLUTION_KEY, 2); // Default to 1920x1080
            resolutionsDropdown.index = Mathf.Clamp(savedIndex, 0, resolutions.Count - 1);

            resolutionsDropdown.RegisterValueChangedCallback((value) => SetResolution(value.newValue));
        }
        else
        {
            Debug.LogError("ResolutionDropdown not found in Settings view");
        }

        if (musicMasterSlider != null)
        {
            // Load saved music volume (default to 100%)
            musicMasterSlider.value = PlayerPrefs.GetFloat(MUSIC_MASTER_KEY, 1f);
            musicMasterSlider.RegisterValueChangedCallback((evt) => SetMusicMasterVolume(evt.newValue));
        }
    }

    private void SetFullscreen(bool enabled)
    {
        Screen.fullScreen = enabled;
        PlayerPrefs.SetInt(FULLSCREEN_KEY, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void SetResolution(string newResolution)
    {
        string[] resolutionArray = newResolution.Split("x");
        int[] valuesIntArray = new int[] { int.Parse(resolutionArray[0]), int.Parse(resolutionArray[1]) };

        Screen.SetResolution(valuesIntArray[0], valuesIntArray[1], fullscreenToggle.value);

        // Save the index
        int index = resolutions.IndexOf(newResolution);
        PlayerPrefs.SetInt(RESOLUTION_KEY, index);
        PlayerPrefs.Save();
    }

    private void SetMusicMasterVolume(float volume)
    {
        // Sets volume in Audio Manager which connects to busses
        Debug.Log("Set Master volume to ");
        AudioManager.Instance.masterVolume = volume;

        PlayerPrefs.SetFloat(MUSIC_MASTER_KEY, volume);
        PlayerPrefs.Save();
    }
}
