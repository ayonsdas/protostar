using UnityEngine;

/// <summary>
/// Cabinet that opens when the telescope puzzle is completed
/// </summary>
public class Cabinet : MonoBehaviour
{
    [Header("Cabinet Models")]
    [SerializeField] private GameObject closedModel;
    [SerializeField] private GameObject openModel;
    
    [Header("Telescope Requirement")]
    [SerializeField] private Telescope requiredTelescope; // Telescope that must complete its puzzle
    
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
        
        Debug.Log("Cabinet opened!");
    }
}
