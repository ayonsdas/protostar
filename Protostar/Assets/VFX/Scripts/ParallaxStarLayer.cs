using UnityEngine;

public class ParallaxStarLayer : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField][Range(0f, 1f)] private float parallaxStrength = 0.95f;

    private Vector3 _lastCameraPos;

    private void Start()
    {
        transform.position = cameraTransform.position;
        _lastCameraPos = cameraTransform.position;
    }

    private void LateUpdate()
    {
        Vector3 delta = cameraTransform.position - _lastCameraPos;
        transform.position += delta * parallaxStrength;
        _lastCameraPos = cameraTransform.position;
    }
}