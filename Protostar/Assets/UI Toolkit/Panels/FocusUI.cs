using UnityEngine;

public class FocusUI : MonoBehaviour
{

    [Header("Settings")]
    // Assign your canvas here
    public Canvas canvas;

    // How fast it fades in/out
    public float fadeSmoothTime = 0.2f;

    // Height offset above the object
    public float heightOffset = 2f;


    private Transform mainCameraTransform;
    private CanvasGroup canvasGroup;
    private bool pendingHide;
    private float targetAlpha;
    private float currentAlpha;
    private float alphaVelocity;

    void Start()
    {
        // 1. Find the Main Camera
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("No Main Camera found! Tag your camera as 'MainCamera'.");
        }

        // 3. Setup the Canvas
        if (canvas)
        {
            // Force World Space settings
            // canvas.renderMode = RenderMode.WorldSpace;

            // Add a CanvasGroup if it's missing (needed for fading)
            if (!canvas.TryGetComponent(out canvasGroup))
            {
                canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            }

            // Start completely invisible
            currentAlpha = targetAlpha = 0f;
            canvasGroup.alpha = 0f;
            canvas.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Handle the Fading Animation
        if (canvasGroup && canvas.gameObject.activeSelf)
        {
            currentAlpha = Mathf.SmoothDamp(currentAlpha, targetAlpha, ref alphaVelocity, fadeSmoothTime);
            canvasGroup.alpha = currentAlpha;

            // If completely invisible, turn off the game object to save performance
            if (pendingHide && currentAlpha <= 0.01f)
            {
                canvas.gameObject.SetActive(false);
                pendingHide = false;
            }
        }
    }

    void LateUpdate()
    {
        if (canvas && canvas.gameObject.activeSelf && mainCameraTransform)
        {
            // Keep canvas at a fixed offset above the object (in camera's local up direction)
            canvas.transform.position = transform.position + mainCameraTransform.up * heightOffset;

            // Match camera rotation
            canvas.transform.rotation = mainCameraTransform.rotation;
        }
    }

    public void ShowUI()
    {
        if (!canvas) return;
        canvas.gameObject.SetActive(true);
        targetAlpha = 1f;
        pendingHide = false;
    }

    public void HideUI()
    {
        targetAlpha = 0f;
        pendingHide = true;
    }
}