using System.Collections;
using UnityEngine;

public class Deadzone : MonoBehaviour
{
    public Transform playerTransform;
    private Quaternion playerStartRot;
    private Vector3 playerStartPos;
    public PlayerController playerController;
    public Rigidbody playerRB;
    public float respawnTime = 2f;
    public bool teleport = false;

    void Start()
    {
        playerStartPos = playerTransform.position + Vector3.up;
        playerStartRot = playerTransform.rotation;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            playerController.SetMovementLocked(true);
            playerRB.useGravity = false;
            playerRB.linearVelocity = Vector3.zero;
            playerRB.angularVelocity = Vector3.zero;

            if(teleport == false)
                StartCoroutine(RespawnPlayer());
            else
            {
                playerTransform.position = playerStartPos;
                playerTransform.rotation = playerStartRot;
                playerController.SetMovementLocked(false);
            }   
        }
    }

    IEnumerator RespawnPlayer()
    {
        float secElapsed = 0f;
        Vector3 currentPos = playerTransform.position;
        Quaternion currentRot = playerTransform.rotation;

        while (secElapsed < respawnTime)
        {
            secElapsed += Time.deltaTime;
            float t = secElapsed / respawnTime;
            playerTransform.position = Vector3.Lerp(currentPos, playerStartPos, t);
            playerTransform.rotation = Quaternion.Lerp(currentRot, playerStartRot, t);
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);
        playerRB.useGravity = true;
        playerTransform.rotation = playerStartRot;
        yield return new WaitForSeconds(.2f);
        playerController.SetMovementLocked(false);
    }
}