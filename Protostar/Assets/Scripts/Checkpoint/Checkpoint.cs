using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    

    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Player"))
        {
            
        }
    }
}
