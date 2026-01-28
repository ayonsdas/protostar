using UnityEngine;
using System.Collections.Generic;

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
    
    [Header("State")]
    [SerializeField] private bool isShifted = false;
    
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
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if a seed entered the zone
        SeedObject seed = other.GetComponent<SeedObject>();
        if (seed != null)
        {
            seedsInZone.Add(seed);
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
