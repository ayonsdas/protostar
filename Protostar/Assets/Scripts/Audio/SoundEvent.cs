using FMODUnity;
using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Sound Event")]
public class SoundEvent : ScriptableObject
{
    public EventReference Event;
    public float Cooldown;
    public bool UseSurfaceType;
    public string SurfaceParameterName = "surfaceId";
}