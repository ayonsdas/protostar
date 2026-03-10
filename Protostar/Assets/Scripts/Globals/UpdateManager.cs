using System;
using UnityEngine;

public class UpdateManager : MonoBehaviour
{
    public static UpdateManager Instance { get; private set; }
    public static Action<float> OnUpdate;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }

        Instance = this;
    }

    void Update()
    {
        OnUpdate?.Invoke(Time.deltaTime);
    }
}
