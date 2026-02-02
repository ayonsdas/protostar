using UnityEngine;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine.Rendering;

/// <summary>
/// Sapling that shifts into a tree when all seeds are in the trigger zone
/// </summary>
public class SaplingPuzzle : MonoBehaviour, IInteractable, IShiftable
{
    [Header("Puzzle Settings")]
    [SerializeField] private int requiredSeeds = 4;
    [SerializeField] private GameObject saplingModel; // The sapling visual
    [SerializeField] private GameObject treeModel; // The tree model to shift to

    [Header("Colliders")]
    [SerializeField] private Collider seedDetectionZone; // Trigger collider for detecting seeds

    [Header("Skybox")]
    [SerializeField] private Material targetSkybox; // The skybox to show after puzzle completion
    
    [Header("Sound")]
    private bool isShifted = false; // Runtime state only
    [field: SerializeField] public EventReference treeGrowSoundEvent { get; private set; }
    [field: SerializeField] public EventReference seedPlantSoundEvent { get; private set; }

    private HashSet<SeedObject> seedsInZone = new HashSet<SeedObject>();
    private bool canInteract = false;

    private void Start()
    {
        // Make sure tree is hidden initially
        if (treeModel != null)
        {
            treeModel.SetActive(false);
        }

        // Make sure sapling is visible
        if (saplingModel != null)
        {
            saplingModel.SetActive(true);
        }
        
        // Set skybox to black at start
        SetBlackSkybox();
    }
    
    private void SetBlackSkybox()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black;
        
        // Enable fog with pure black - very aggressive
        RenderSettings.fog = true;
        RenderSettings.fogColor = Color.black;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 0f;  // Start immediately
        RenderSettings.fogEndDistance = 20f;   // Very short distance
        
        // Set camera background to black
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = Color.black;
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
        }
        
        DynamicGI.UpdateEnvironment();
    }
    
    private void SetTargetSkybox()
    {
        if (targetSkybox != null)
        {
            RenderSettings.skybox = targetSkybox;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            
            // Disable fog when skybox is enabled
            RenderSettings.fog = false;
            
            // Reset camera to skybox mode
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.clearFlags = CameraClearFlags.Skybox;
            }
            
            DynamicGI.UpdateEnvironment();
            Debug.Log("Skybox changed to target skybox");
        }
        else
        {
            Debug.LogWarning("Target skybox material is not assigned!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if a seed entered the zone
        SeedObject seed = other.GetComponent<SeedObject>();
        if (seed != null)
        {
            seedsInZone.Add(seed);
            RuntimeManager.PlayOneShot(seedPlantSoundEvent, seed.transform.position);
            Debug.Log($"Seed entered zone. Total seeds: {seedsInZone.Count}/{requiredSeeds}");
            UpdateInteractableState();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if a seed left the zone
        SeedObject seed = other.GetComponent<SeedObject>();
        if (seed != null)
        {
            seedsInZone.Remove(seed);
            Debug.Log($"Seed left zone. Total seeds: {seedsInZone.Count}/{requiredSeeds}");
            UpdateInteractableState();
        }
    }

    private void UpdateInteractableState()
    {
        // Can only interact when all seeds are present and not already shifted
        canInteract = seedsInZone.Count >= requiredSeeds && !isShifted;

        if (canInteract)
        {
            Debug.Log("All seeds collected! You can now interact with the sapling.");
        }
    }

    public void Interact(GameObject interactor)
    {
        Debug.Log($"[SaplingPuzzle] Interact called: canInteract={canInteract}, isShifted={isShifted}, seedsInZone.Count={seedsInZone.Count}, requiredSeeds={requiredSeeds}");
        
        if (canInteract)
        {
            Debug.Log("Attempting to shift sapling...");
            // Try to shift via IShiftable interface
            Shift(1); // Shift forward
        }
        else if (isShifted)
        {
            Debug.Log("Sapling has already been shifted to a tree.");
        }
        else
        {
            Debug.Log($"Need all {requiredSeeds} seeds in the zone. Currently have {seedsInZone.Count}.");
        }
    }

    private void PlaySFX()
    {
        RuntimeManager.PlayOneShot(treeGrowSoundEvent, gameObject.transform.position);
    }

    // IShiftable implementation
    public void Shift(int direction)
    {
        Debug.Log($"Shift called. canInteract={canInteract}, isShifted={isShifted}");

        if (!canInteract || isShifted)
        {
            Debug.Log("Cannot shift - requirements not met");
            return;
        }

        isShifted = true;
        PlaySFX();
        Debug.Log("Starting shift process...");


        // Hide sapling
        if (saplingModel != null)
        {
            saplingModel.SetActive(false);
            Debug.Log("Sapling model hidden");
        }
        else
        {
            Debug.LogWarning("Sapling model is null!");
        }

        // Hide/destroy all seeds
        Debug.Log($"Hiding {seedsInZone.Count} seeds");
        foreach (var seed in seedsInZone)
        {
            if (seed != null)
            {
                seed.gameObject.SetActive(false);
                Debug.Log($"Hidden seed: {seed.name}");
                // Or use Destroy(seed.gameObject) if you want to permanently remove them
            }
        }
        seedsInZone.Clear();

        // Show tree
        if (treeModel != null)
        {
            treeModel.SetActive(true);
            Debug.Log("Tree model shown");
        }
        else
        {
            Debug.LogWarning("Tree model is null!");
        }

        Debug.Log("Sapling shifted into tree! Seeds consumed.");
        
        // Change skybox to target skybox
        SetTargetSkybox();

        // Disable the seed detection zone so no more seeds affect it
        if (seedDetectionZone != null)
        {
            seedDetectionZone.enabled = false;
        }
    }

    public bool CanShift()
    {
        return canInteract && !isShifted;
    }

    public int GetState()
    {
        return isShifted ? 1 : 0;
    }
}
