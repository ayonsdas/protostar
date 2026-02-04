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
    [SerializeField] private GameObject sun; // The sun/directional light to hide/show
    
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
        
        // Check for seeds already in the zone at startup
        DetectInitialSeeds();
        
        // Set skybox to black at start
        SetBlackSkybox();
    }
    
    private void DetectInitialSeeds()
    {
        if (seedDetectionZone != null)
        {
            // Find all SeedObject components in the scene
            SeedObject[] allSeeds = FindObjectsOfType<SeedObject>();
            
            foreach (SeedObject seed in allSeeds)
            {
                // Check if the seed's collider is within the detection zone
                Collider seedCollider = seed.GetComponent<Collider>();
                if (seedCollider != null && seedDetectionZone.bounds.Intersects(seedCollider.bounds))
                {
                    seedsInZone.Add(seed);
                    Debug.Log($"[SaplingPuzzle] Found seed already in zone at startup: {seed.name}");
                }
            }
            
            Debug.Log($"[SaplingPuzzle] Initial seed count: {seedsInZone.Count}/{requiredSeeds}");
            UpdateInteractableState();
        }
    }
    
    private void SetBlackSkybox()
    {
        // Set skybox to null for pure black
        RenderSettings.skybox = null;
        
        // Set ambient lighting to black
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black;
        RenderSettings.ambientIntensity = 0f;
        
        // Disable reflection probes and environment reflections
        RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
        RenderSettings.customReflectionTexture = null;
        RenderSettings.reflectionIntensity = 0f;
        
        // Disable fog completely
        RenderSettings.fog = false;
        
        // Disable subtractive ambient (prevents light bleeding)
        RenderSettings.subtractiveShadowColor = Color.black;
        
        // Hide the sun
        if (sun != null)
        {
            sun.SetActive(false);
        }
        
        // Set camera background to pure black with no environment influence
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
            
            // Show the sun
            if (sun != null)
            {
                sun.SetActive(true);
            }
            
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
        Debug.Log($"[SaplingPuzzle] OnTriggerEnter called! Collider name: {other.gameObject.name}");
        
        // Check if a seed entered the zone - check parent too in case collider is on child
        SeedObject seed = other.GetComponentInParent<SeedObject>();
        if (seed != null)
        {
            seedsInZone.Add(seed);
            
            // Try to play sound, but don't let it break gameplay
            try
            {
                RuntimeManager.PlayOneShot(seedPlantSoundEvent, seed.transform.position);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to play seed plant sound: {e.Message}");
            }
            
            Debug.Log($"Seed entered zone. Total seeds: {seedsInZone.Count}/{requiredSeeds}");
            UpdateInteractableState();
        }
        else
        {
            Debug.Log($"[SaplingPuzzle] Object that entered has no SeedObject component (checked parent too)");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"[SaplingPuzzle] OnTriggerExit called! Collider name: {other.gameObject.name}");
        
        // Check if a seed left the zone - check parent too
        SeedObject seed = other.GetComponentInParent<SeedObject>();
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
        bool hadEnoughSeeds = seedsInZone.Count >= requiredSeeds;
        bool notShifted = !isShifted;
        canInteract = hadEnoughSeeds && notShifted;

        Debug.Log($"[SaplingPuzzle] UpdateInteractableState: seedsInZone.Count={seedsInZone.Count}, requiredSeeds={requiredSeeds}, hadEnoughSeeds={hadEnoughSeeds}, notShifted={notShifted}, canInteract={canInteract}");

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
        try
        {
            RuntimeManager.PlayOneShot(treeGrowSoundEvent, gameObject.transform.position);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to play tree grow sound: {e.Message}");
        }
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
