using UnityEngine;

public class GravityController : MonoBehaviour
{
    [Header("Gravity Settings")]
    public Vector3 gravityDirection = new Vector3(0, -1, 0); // Default: down
    public float gravityStrength = 9.81f; // Earth gravity magnitude
    public float transitionDuration = 1f; // How long gravity transitions take
    
    [Header("Debug")]
    public bool showGravityDirection = true;
    public float arrowLength = 2f;

    private static GravityController instance;
    private Vector3 targetGravityDirection;
    private float transitionProgress = 1f; // 1 = transition complete
    private Vector3 previousGravityDirection;
    
    public static GravityController Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GravityController>();
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
        
        // Initialize target to current direction
        targetGravityDirection = gravityDirection.normalized;
        previousGravityDirection = targetGravityDirection;
    }

    void Update()
    {
        // Smoothly transition gravity direction
        if (transitionProgress < 1f)
        {
            transitionProgress += Time.deltaTime / transitionDuration;
            transitionProgress = Mathf.Clamp01(transitionProgress);
            
            // Slerp for smooth rotation between directions
            gravityDirection = Vector3.Slerp(previousGravityDirection, targetGravityDirection, transitionProgress);
        }
    }

    public Vector3 GetGravity()
    {
        return gravityDirection.normalized * gravityStrength;
    }

    public void SetGravityDirection(Vector3 newDirection)
    {
        previousGravityDirection = gravityDirection;
        targetGravityDirection = newDirection.normalized;
        transitionProgress = 0f;
    }

    public void SetGravityDirection(float x, float y, float z)
    {
        SetGravityDirection(new Vector3(x, y, z));
    }

    public void RotateGravityAroundAxis(Vector3 axis, float degrees)
    {
        Quaternion rotation = Quaternion.AngleAxis(degrees, axis);
        Vector3 newDirection = rotation * gravityDirection;
        SetGravityDirection(newDirection);
    }

    void OnDrawGizmos()
    {
        if (showGravityDirection)
        {
            Gizmos.color = Color.yellow;
            Vector3 gravity = GetGravity();
            Gizmos.DrawRay(transform.position, gravity.normalized * arrowLength);
            
            // Draw arrow head
            Vector3 arrowTip = transform.position + gravity.normalized * arrowLength;
            Vector3 right = Vector3.Cross(gravity, Vector3.up).normalized * 0.3f;
            if (right == Vector3.zero) right = Vector3.Cross(gravity, Vector3.forward).normalized * 0.3f;
            Vector3 up = Vector3.Cross(right, gravity).normalized * 0.3f;
            
            Gizmos.DrawLine(arrowTip, arrowTip - gravity.normalized * 0.5f + right);
            Gizmos.DrawLine(arrowTip, arrowTip - gravity.normalized * 0.5f - right);
        }
    }
}
