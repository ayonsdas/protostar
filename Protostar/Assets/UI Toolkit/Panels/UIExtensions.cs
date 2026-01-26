using Unity.VisualScripting.ReorderableList.Element_Adder_Menu;
using UnityEngine;
using UnityEngine.UIElements;

public static class UIExtensions
{
    public static void Display(this VisualElement element, bool enabled)
    {
        if(element == null) return;
        element.style.display = enabled ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
