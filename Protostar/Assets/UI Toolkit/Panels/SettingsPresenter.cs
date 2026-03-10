using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SettingsPresenter
{
    private const float VOLUME_SCALE = 100f;
    private List<string> resolutions = new List<string>()
    {
        "3840x2160",
        "2560x1440",
        "1920x1080",
        "1600x900",
        "1280x720"
    };

    public Action BackAction { set { UIHelper.RegisterButton(backButton, value); } }
    public Action ReturnToMainMenuAction { set { UIHelper.RegisterButton(mainMenuButton, value); } }
    public Action ControlsAction { set { UIHelper.RegisterButton(controlsButton, value); } }

    private Button backButton;
    private Button mainMenuButton;
    private Button controlsButton;
    private Toggle fullscreenToggle;
    private DropdownField resolutionsDropdown;
    private Slider masterVolumeSlider;
    private Slider mouseSensitivitySlider;

    public SettingsPresenter(VisualElement root)
    {
        if (root == null)
        {
            Debug.LogError("Settings root is null");
            return;
        }
        backButton = root.Q<Button>("BackButton");
        mainMenuButton = root.Q<Button>("MainMenuButton");
        controlsButton = root.Q<Button>("ControlsButton");
        fullscreenToggle = root.Q<Toggle>("FullscreenToggle");
        resolutionsDropdown = root.Q<DropdownField>("ResolutionDropdown");
        masterVolumeSlider = root.Q<Slider>("MasterVolumeSlider");
        mouseSensitivitySlider = root.Q<Slider>("MouseSensitivitySlider");

        if (backButton == null)
        {
            Debug.LogError("BackButton not found in Settings");
        }
        if (fullscreenToggle != null)
        {
            fullscreenToggle.value = PlayerPrefs.GetInt(PlayerPrefKeys.FULLSCREEN_KEY, Screen.fullScreen ? 1 : 0) == 1;
            fullscreenToggle.RegisterCallback<MouseUpEvent>((evt) => { SetFullscreen(fullscreenToggle.value); }, TrickleDown.TrickleDown);
        }
        else
        {
            Debug.LogWarning("[SettingsPresenter] FullscreenToggle not found in Settings view");
        }

        if (resolutionsDropdown != null)
        {
            resolutionsDropdown.choices = resolutions;
            int savedIndex = PlayerPrefs.GetInt(PlayerPrefKeys.RESOLUTION_KEY, 2);
            resolutionsDropdown.index = Mathf.Clamp(savedIndex, 0, resolutions.Count - 1);
            resolutionsDropdown.RegisterValueChangedCallback((value) => SetResolution(value.newValue));
        }
        else
        {
            Debug.LogError("[SettingsPresenter] ResolutionDropdown not found in Settings view");
        }
        if (masterVolumeSlider != null)
        {
            if (PlayerPrefs.HasKey(PlayerPrefKeys.MASTER_VOLUME_KEY))
            {
                masterVolumeSlider.value = PlayerPrefs.GetFloat(PlayerPrefKeys.MASTER_VOLUME_KEY) * VOLUME_SCALE;
            }
            masterVolumeSlider.RegisterValueChangedCallback((evt) => SetMusicMasterVolume(evt.newValue));
        }
        else
        {
            Debug.LogError("[SettingsPresenter] MasterVolumeSlider not found in Settings view");
        }

        if (mouseSensitivitySlider != null)
        {
            if (PlayerPrefs.HasKey(PlayerPrefKeys.MOUSE_SENSETIVITY_KEY))
            {
                mouseSensitivitySlider.value = PlayerPrefs.GetFloat(PlayerPrefKeys.MOUSE_SENSETIVITY_KEY);
            }
            mouseSensitivitySlider.RegisterValueChangedCallback((evt) => SetMouseSensetivity(evt.newValue));
        }
        else
        {
            Debug.LogError("[SettingsPresenter] MouseSensitivitySlider not found in Settings view");
        }
    }

    private void SetFullscreen(bool enabled)
    {
        Screen.fullScreen = enabled;
        PlayerPrefs.SetInt(PlayerPrefKeys.FULLSCREEN_KEY, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }
    private void SetResolution(string newResolution)
    {
        string[] resolutionArray = newResolution.Split("x");
        int[] valuesIntArray = new int[] { int.Parse(resolutionArray[0]), int.Parse(resolutionArray[1]) };
        Screen.SetResolution(valuesIntArray[0], valuesIntArray[1], fullscreenToggle.value);
        int index = resolutions.IndexOf(newResolution);
        PlayerPrefs.SetInt(PlayerPrefKeys.RESOLUTION_KEY, index);
        PlayerPrefs.Save();
    }
    private void SetMusicMasterVolume(float volume)
    {
        float normalizedVolume = volume / VOLUME_SCALE;
        PlayerPrefs.SetFloat(PlayerPrefKeys.MASTER_VOLUME_KEY, normalizedVolume);
        PlayerPrefs.Save();
        AudioManager.Instance.SetBusVolume(AudioBus.Master, normalizedVolume);
    }
    private void SetMouseSensetivity(float sensitivity)
    {
        SettingsManager.Instance.SetMouseSensitivity(sensitivity);
    }
}