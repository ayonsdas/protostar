using FMODUnity;
using UnityEngine;

public class PlatformSurface : MonoBehaviour
{
    [SerializeField] private SurfaceType surfaceType = SurfaceType.Default;

    public float ParameterValue => (float)surfaceType;
}
