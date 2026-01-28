using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class SimpleWorldUI : MonoBehaviour {
    
    [Header("Settings")]
    // Assign your canvas here
    public Canvas canvas;
    
    // Distance to trigger the text
    [Min(0f)] public float triggerRadius = 3f;
    
    // How fast it fades in/out
    public float fadeSmoothTime = 0.2f;

    // Internal Variables (You don't need to touch these)
    private Transform mainCameraTransform;
    private CanvasGroup canvasGroup;
    private bool pendingHide;
    private float targetAlpha;
    private float currentAlpha;
    private float alphaVelocity;

    void Start() {
        // 1. Auto-find the Main Camera
        if (Camera.main != null) {
            mainCameraTransform = Camera.main.transform;
        } else {
            Debug.LogError("No Main Camera found! Tag your camera as 'MainCamera'.");
        }

        // 2. Setup the Trigger
        SphereCollider trigger = GetComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = triggerRadius;

        // 3. Setup the Canvas
        if (canvas) {
            // Force World Space settings
            canvas.renderMode = RenderMode.WorldSpace;
            
            // Add a CanvasGroup if it's missing (needed for fading)
            if (!canvas.TryGetComponent(out canvasGroup)) {
                canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            }
            
            // Start completely invisible
            currentAlpha = targetAlpha = 0f;
            canvasGroup.alpha = 0f;
            canvas.gameObject.SetActive(false);
        }
    }

    void Update() {
        // Handle the Fading Animation
        if (canvasGroup && canvas.gameObject.activeSelf) {
            currentAlpha = Mathf.SmoothDamp(currentAlpha, targetAlpha, ref alphaVelocity, fadeSmoothTime);
            canvasGroup.alpha = currentAlpha;

            // If completely invisible, turn off the game object to save performance
            if (pendingHide && currentAlpha <= 0.01f) {
                canvas.gameObject.SetActive(false);
                pendingHide = false;
            }
        }
    }

    void LateUpdate() {
        // Rotate UI to face camera
        if (canvas && canvas.gameObject.activeSelf && mainCameraTransform) {
            canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - mainCameraTransform.position);
        }
    }

    void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")) {
            ShowUI();
        }
    }

    void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player")) {
            HideUI();
        }
    }

    void ShowUI() {
        if (!canvas) return;
        canvas.gameObject.SetActive(true);
        targetAlpha = 1f;
        pendingHide = false;
    }

    void HideUI() {
        targetAlpha = 0f;
        pendingHide = true;
    }
    
    // Show range in editor
    private void OnDrawGizmosSelected() {
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}