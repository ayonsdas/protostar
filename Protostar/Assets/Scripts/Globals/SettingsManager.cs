using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }
    public float MouseSensitivity { get; private set; } = 3f;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log("[SettingsManager] Duplicate Settings manager, destroying");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        MouseSensitivity = PlayerPrefs.GetFloat(PlayerPrefKeys.MOUSE_SENSETIVITY_KEY, MouseSensitivity);
    }

    public void SetMouseSensitivity(float value)
    {
        MouseSensitivity = value;
        PlayerPrefs.SetFloat(PlayerPrefKeys.MOUSE_SENSETIVITY_KEY, value);
    }
}
