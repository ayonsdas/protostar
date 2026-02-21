using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;

public class VideoController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public GameObject videoCanvas;
    
    private PlayerInput playerInput;
    
    void Start()
    {
        videoPlayer.loopPointReached += CloseVideoUI;
        videoPlayer.started += OnVideoStarted;
        
        // Find player controller
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerInput = player.GetComponent<PlayerInput>();
        }
    }

    void OnVideoStarted(VideoPlayer vp)
    {
        // Lock player movement when video starts
        if (playerInput != null)
        {
            playerInput.currentActionMap.Disable();
            Debug.Log("[AutoCloseVideo] Player input disabled");
        }
    }
    
    void CloseVideoUI(VideoPlayer vp)
    {
        videoCanvas.SetActive(false);
        videoPlayer.Stop();
        
        // Unlock player movement when video ends
        if (playerInput != null)
        {
            playerInput.currentActionMap.Enable();
            Debug.Log("[AutoCloseVideo] Player input enabled");
        }
    }
    
    void OnDestroy()
    {
        videoPlayer.loopPointReached -= CloseVideoUI;
        videoPlayer.started -= OnVideoStarted;
        
        // Ensure movement is unlocked if script is destroyed
        if (playerInput != null)
        {
            playerInput.currentActionMap.Enable();
        }
    }

    public void SkipVideo()
    {
        Debug.Log("SKIP PRESSED");
        CloseVideoUI(videoPlayer);
    }
}