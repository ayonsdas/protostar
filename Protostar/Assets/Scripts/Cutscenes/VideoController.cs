using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;

public class VideoController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public GameObject videoCanvas;
        
    void Start()
    {
        videoPlayer.loopPointReached += CloseVideoUI;
        videoPlayer.started += OnVideoStarted;
    }

    void OnVideoStarted(VideoPlayer vp)
    {
        // Lock player movement when video starts
        GameStateManager.Instance.SetState(GameState.Cutscene);
    }
    
    void CloseVideoUI(VideoPlayer vp)
    {
        videoCanvas.SetActive(false);
        videoPlayer.Stop();

        // Unlock player movement when video ends
        GameStateManager.Instance.RevertState();
    }
    
    void OnDestroy()
    {
        videoPlayer.loopPointReached -= CloseVideoUI;
        videoPlayer.started -= OnVideoStarted;
    }

    public void SkipVideo()
    {
        Debug.Log("SKIP PRESSED");
        CloseVideoUI(videoPlayer);
    }
}