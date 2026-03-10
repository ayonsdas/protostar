using UnityEngine;


public class GridArrowSpawner : MonoBehaviour
{
    [Header("References")]
    public ParticleSystem arrowParticleSystem;

    [Header("Grid Settings")]
    public int columns = 4;
    public int rows = 4;
    public float spacingX = 0.8f;
    public float spacingZ = 0.8f;

    [Header("Timing")]
    public float repeatInterval = 1.5f;   

    private ParticleSystem.EmitParams _emitParams;
    private float _timer;

    void Start()
    {
        var emission = arrowParticleSystem.emission;
        emission.enabled = false;

        _timer = repeatInterval; // emit on first frame
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= repeatInterval)
        {
            _timer = 0f;
            EmitGrid();
        }
    }

    void EmitGrid()
    {
        // calculate the offset so the grid is centered on the GameObject
        float totalW = (columns - 1) * spacingX;
        float totalD = (rows    - 1) * spacingZ;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                float x = col * spacingX - totalW * 0.5f;
                float z = row * spacingZ - totalD * 0.5f;

                _emitParams.position = transform.position + new Vector3(x, 0f, z);
                _emitParams.applyShapeToPosition = true;

                arrowParticleSystem.Emit(_emitParams, 1);
            }
        }
    }

    // Draw the grid in the editor so you can see it while placing
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.2f, 1f, 0.5f);
        float totalW = (columns - 1) * spacingX;
        float totalD = (rows    - 1) * spacingZ;

        for (int row = 0; row < rows; row++)
            for (int col = 0; col < columns; col++)
            {
                float x = col * spacingX - totalW * 0.5f;
                float z = row * spacingZ - totalD * 0.5f;
                Gizmos.DrawWireSphere(transform.position + new Vector3(x, 0f, z), 0.1f);
            }
    }
}