using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Video;
using UnityEngine.Audio;

public class SimpleVideoPlayer : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private VideoPlayer _videoPlayer;
    [SerializeField] private AudioSetting _setting;//


    private void Start()
    {
        PauseVideo();
    }


    // 화면 클릭 시 호출 (IPointerClickHandler 인터페이스)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_videoPlayer.isPlaying)
            PauseVideo();
        else
            PlayVideo();
    }



    private void PlayVideo()
    {
        _setting.MuteBGM();
        _videoPlayer.Play();
    }



    public void PauseVideo()
    {
        _setting.ApplyVolumes();
        _videoPlayer.Pause();
    }
}
