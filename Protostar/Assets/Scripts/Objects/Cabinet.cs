using FMODUnity;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Cabinet that opens when the telescope puzzle is completed
/// Can be interacted with to end the demo
/// </summary>
public class Cabinet : MonoBehaviour, IInteractable
{
    [Header("Cabinet Models")]
    [SerializeField] private GameObject closedModel;
    [SerializeField] private GameObject openModel;
    [SerializeField] private GameObject bookModel; // Book to show when cabinet opens

    [Header("Telescope Requirement")]
    [SerializeField] private Telescope requiredTelescope; // Telescope that must complete its puzzle
    
    [Header("Demo End Trigger")]
    [SerializeField] private string nextSceneName = "MainMenu"; // Scene to load when demo ends
    
    [Header("Sound Effects")]
    [SerializeField] private EventReference openEventReference;

    private bool isOpen = false;
    private bool wasLightOn = false;

    private void Start()
    {
        // Start in closed state
        if (closedModel != null)
        {
            closedModel.SetActive(true);
        }

        if (openModel != null)
        {
            openModel.SetActive(false);
        }
        
        // Hide book initially
        if (bookModel != null)
        {
            bookModel.SetActive(false);
        }
    }

    private void Update()
    {
        // Check if telescope puzzle is complete
        if (!isOpen && requiredTelescope != null)
        {
            bool isLightOn = requiredTelescope.IsLightOn();

            // Open cabinet when light turns on for the first time
            if (isLightOn && !wasLightOn)
            {
                OpenCabinet();
            }

            wasLightOn = isLightOn;
        }
    }

    private void OpenCabinet()
    {
        isOpen = true;

        // Hide closed model
        if (closedModel != null)
        {
            closedModel.SetActive(false);
        }

        // Show open model
        if (openModel != null)
        {
            openModel.SetActive(true);
        }
        
        // Show book inside cabinet
        if (bookModel != null)
        {
            bookModel.SetActive(true);
            Debug.Log("Book appeared in cabinet!");
        }

        AudioManager.Instance.PlayOneShot(openEventReference, gameObject.transform.position);
        Debug.Log("Cabinet opened! You can now interact with it to complete the demo.");
    }
    
    public void Interact(GameObject interactor)
    {
        // Only allow interaction if cabinet is open
        if (isOpen)
        {
            EndDemo();
        }
        else
        {
            Debug.Log("The cabinet is still locked. Complete the telescope puzzle first!");
        }
    }
    
    private void EndDemo()
    {
        Debug.Log("Demo Complete! Closing game...");
        
        // Show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Quit the application
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
