using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class AutoCloseVideo : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public GameObject videoCanvas;
    
    void Start()
    {
        videoPlayer.loopPointReached += CloseVideoUI;
    }
    
    void CloseVideoUI(VideoPlayer vp)
    {
        videoCanvas.SetActive(false);
        videoPlayer.Stop();
    }
    
    void OnDestroy()
    {
        videoPlayer.loopPointReached -= CloseVideoUI;
    }
}