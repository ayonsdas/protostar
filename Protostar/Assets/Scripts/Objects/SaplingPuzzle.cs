using UnityEngine;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine.Rendering;

/// <summary>
/// Sapling that shifts into a tree when all four seed slots are filled.
/// </summary>
public class SaplingPuzzle : MonoBehaviour, IEngageable, IShiftable, IInteractionCandidate
{
    [Header("Puzzle Settings")]
    [SerializeField] private SeedSlot[] seedSlots; // Assign exactly 4 in the Inspector
    [SerializeField] private GameObject saplingModel;
    [SerializeField] private GameObject treeModel;

    [Header("Skybox")]
    [SerializeField] private bool startWithBlackSkybox = true; // Toggle whether to start with black skybox
    [SerializeField] private Material targetSkybox;
    [SerializeField] private GameObject sun;
    
    [Header("Sound")]
    private bool isShifted = false;
    [field: SerializeField] public EventReference treeGrowSoundEvent { get; private set; }

    private const string UNSHIFTABLE_INSPECT_MESSAGE = "This tree sapling needs more energy to grow, it can't be shifted yet.";
    private const string SHIFTABLE_INSPECT_MESSAGE = "The maleable course of time has been altered for this tree, it's ready to be shifted!";
    private const string SHIFTED_INSPECT_MESSAGE = "The omni-tree you grew bears the leaves of a whole new universe!";

    private bool CanShift => canInteract && !isShifted;

    private bool canInteract = false;
    private bool _engaged = false;

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

        // Subscribe to each slot's change event
        if (seedSlots != null)
        {
            foreach (var slot in seedSlots)
            {
                if (slot != null)
                {
                    slot.OnSlotChanged += UpdateInteractableState;
                }
            }
        }

        UpdateInteractableState();
        
        // Set skybox to black at start (if enabled)
        if (startWithBlackSkybox)
        {
            SetBlackSkybox();
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe to avoid leaks
        if (seedSlots != null)
        {
            foreach (var slot in seedSlots)
            {
                if (slot != null)
                {
                    slot.OnSlotChanged -= UpdateInteractableState;
                }
            }
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

    private int FilledSlotCount()
    {
        if (seedSlots == null) return 0;
        int count = 0;
        foreach (var slot in seedSlots)
        {
            if (slot != null && slot.IsFilled) count++;
        }
        return count;
    }

    private bool AllSlotsFilled()
    {
        if (seedSlots == null || seedSlots.Length == 0) return false;
        foreach (var slot in seedSlots)
        {
            if (slot == null || !slot.IsFilled) return false;
        }
        return true;
    }

    private void UpdateInteractableState()
    {
        bool allFilled = AllSlotsFilled();
        bool notShifted = !isShifted;
        canInteract = allFilled && notShifted;

        Debug.Log($"[SaplingPuzzle] UpdateInteractableState: filled={FilledSlotCount()}/{(seedSlots != null ? seedSlots.Length : 0)}, allFilled={allFilled}, notShifted={notShifted}, canInteract={canInteract}");

        if (canInteract)
        {
            Debug.Log("[SaplingPuzzle] All seed slots filled! You can now shift the sapling.");
        }
    }

    // IEngageable implementation
    public void Engage(GameObject interactor)
    {
        _engaged = true;
        Debug.Log($"[SaplingPuzzle] Engaged. canInteract={canInteract}, isShifted={isShifted}");
        if (canInteract)
        {
            Debug.Log("[SaplingPuzzle] All slots filled! Press Shift to grow the tree!");
        }
        else if (isShifted)
        {
            Debug.Log("[SaplingPuzzle] Sapling has already been shifted to a tree.");
        }
        else
        {
            Debug.Log($"[SaplingPuzzle] Need all {(seedSlots != null ? seedSlots.Length : 0)} seed slots filled. Currently have {FilledSlotCount()}.");
        }
    }

    public void Disengage(GameObject interactor)
    {
        _engaged = false;
        Debug.Log("[SaplingPuzzle] Disengaged");
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

        // Consume all seeds from slots
        if (seedSlots != null)
        {
            foreach (var slot in seedSlots)
            {
                if (slot != null)
                {
                    slot.ConsumeObject();
                }
            }
        }
        Debug.Log("All seeds consumed from slots.");

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
    }

    public int GetState()
    {
        return isShifted ? 1 : 0;
    }

    public void CollectOptions(PlayerInteractionContext context, List<InteractionOption> options)
    {
        if(CanShift)
        {
            options.Add(InteractionBuilder.Create(
                InteractionType.Shift,
                this
            ));
            options.Add(InteractionBuilder.Create(
                InteractionType.Inspect,
                this,
                SHIFTABLE_INSPECT_MESSAGE
            ));
        }

        if(isShifted)
        {
            options.Add(InteractionBuilder.Create(
                InteractionType.Inspect,
                this,
                SHIFTED_INSPECT_MESSAGE
            ));
        }

        if(!CanShift)
        {
            options.Add(InteractionBuilder.Create(
                InteractionType.Inspect,
                this,
                UNSHIFTABLE_INSPECT_MESSAGE
            ));
        }
    }
}
