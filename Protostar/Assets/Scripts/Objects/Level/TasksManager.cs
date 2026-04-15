using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TasksManager : MonoBehaviour
{
    [SerializeField] private Image OpenList;
    [SerializeField] private Image ClosedList;

    [SerializeField] private Sprite noCheck;
    [SerializeField] private Sprite oneCheck;
    [SerializeField] private Sprite twoCheck;
    [SerializeField] private Sprite threeCheck;
    [SerializeField] private Sprite oneThreeCheck;
    [SerializeField] private Sprite twoThreeCheck;
    [SerializeField] private Sprite twoOnlyCheck;
    [SerializeField] private Sprite threeOnlyCheck;


    [SerializeField] private CutsceneInteractionItem Item1;
    [SerializeField] private CutsceneInteractionItem Item2;
    [SerializeField] private CutsceneInteractionItem Item3;

    private bool item1Crossed = false;
    private bool item2Crossed = false;
    private bool item3Crossed = false;

    private bool isOpen;

    void OnEnable()
    {
        Item1.OnInteract += TestMethod;
        Item2.OnInteract += TestMethod;
        Item3.OnInteract += TestMethod;
    }

    void OnDisable()
    {
        Item1.OnInteract -= TestMethod;
        Item2.OnInteract -= TestMethod;
        Item3.OnInteract -= TestMethod;
    }
    

    private void Start()
    {
        OpenList.sprite = noCheck;
        isOpen = true;
    }

    private void Update()
    {
        if(Keyboard.current.tabKey.wasPressedThisFrame)
        {
            SwitchStates();
        }
    }

    void TestMethod(CutsceneInteractionItem.InteractItems interactItem)
    {
        if(interactItem == CutsceneInteractionItem.InteractItems.Item1)
        {
            item1Crossed = true;
        }
        else if(interactItem == CutsceneInteractionItem.InteractItems.Item2)
        {
            item2Crossed = true;
        }
        else if(interactItem == CutsceneInteractionItem.InteractItems.Item3)
        {
            item3Crossed = true;
        }
        
        // update OpenList sprites
        switch(item1Crossed, item2Crossed, item3Crossed)
        {
            case (true, true, true):
                OpenList.sprite = threeCheck;
                break;
            case (true, true, false):
                OpenList.sprite = twoCheck;
                break;
            case (true, false, true):
                OpenList.sprite = oneThreeCheck;
                break;
            case (false, true, true):
                OpenList.sprite = twoThreeCheck;
                break; 
            case (true, false, false):
                OpenList.sprite = oneCheck;
                break; 
            case (false, true, false):
                OpenList.sprite = twoOnlyCheck;
                break; 
            case (false, false, true):
                OpenList.sprite = threeOnlyCheck;
                break;          
        }

        if(isOpen == false)
        {
            SwitchStates();
        }
    }

    void SwitchStates()
    {
        if(isOpen)
        {
            OpenList.gameObject.SetActive(false);
            ClosedList.gameObject.SetActive(true);
            isOpen = false;
        }
        else
        {
            ClosedList.gameObject.SetActive(false);
            OpenList.gameObject.SetActive(true); 
            isOpen = true;         
        }
    }
}
