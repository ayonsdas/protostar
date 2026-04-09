using UnityEngine;

[CreateAssetMenu(menuName = "Cutscene/Image Cutscene")]
public class ImageCutscene : ScriptableObject
{
    public Sprite[] Frames;
    public int Length => Frames.Length;
}