using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public GameObject videoCanvas;
    
    private PlayerController playerController;
    
    void Start()
    {
        videoPlayer.loopPointReached += CloseVideoUI;
        videoPlayer.started += OnVideoStarted;
        
        // Find player controller
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
        }
    }

    void OnVideoStarted(VideoPlayer vp)
    {
        // Lock player movement when video starts
        if (playerController != null)
        {
            playerController.SetMovementLocked(true);
            Debug.Log("[AutoCloseVideo] Player movement locked");
        }
    }
    
    void CloseVideoUI(VideoPlayer vp)
    {
        videoCanvas.SetActive(false);
        videoPlayer.Stop();
        
        // Unlock player movement when video ends
        if (playerController != null)
        {
            playerController.SetMovementLocked(false);
            Debug.Log("[AutoCloseVideo] Player movement unlocked");
        }
    }
    
    void OnDestroy()
    {
        videoPlayer.loopPointReached -= CloseVideoUI;
        videoPlayer.started -= OnVideoStarted;
        
        // Ensure movement is unlocked if script is destroyed
        if (playerController != null)
        {
            playerController.SetMovementLocked(false);
        }
    }

    public void SkipVideo()
    {
        Debug.Log("SKIP PRESSED");
        CloseVideoUI(videoPlayer);
    }
}