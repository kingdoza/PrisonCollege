using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;

public class Monitor : MonoBehaviour
{
    [SerializeField] private VideoPlayer _workingVideo;
    [SerializeField] private VideoPlayer _hackingVideo;
    [SerializeField] private VideoPlayer _gamingVideo;
    [SerializeField] private MeshRenderer _renderer;
    [SerializeField] private SoundData _typingSD;
    private VideoPlayer _currentVideo;
    private Dictionary<DisplayState, VideoPlayer> _stateVideoDic = new();
    private SoundEmitter _emitter;



    private void Awake()
    {
        //_renderer = GetComponent<MeshRenderer>();
        _stateVideoDic.Add(DisplayState.Off, null);
        _stateVideoDic.Add(DisplayState.Working, _workingVideo);
        _stateVideoDic.Add(DisplayState.Hacking, _hackingVideo);
        _stateVideoDic.Add(DisplayState.Gaming, _gamingVideo);
        foreach (VideoPlayer video in _stateVideoDic.Values)
        {
            video?.Stop();
        }
        ChangeDisplay(DisplayState.Off);
    }



    //private void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Q))
    //    {
    //        ShowDisplay(DisplayState.Off);
    //    }
    //    else if (Input.GetKeyDown(KeyCode.W))
    //    {
    //        ShowDisplay(DisplayState.Working);
    //    }
    //    else if (Input.GetKeyDown(KeyCode.E))
    //    {
    //        ShowDisplay(DisplayState.Hacking);
    //    }
    //    else if (Input.GetKeyDown(KeyCode.R))
    //    {
    //        ShowDisplay(DisplayState.Gaming);
    //    }
    //}



    public void PauseDisplay()
    {
        _currentVideo?.Pause();
        _emitter.GetComponent<AudioSource>().Pause();
    }



    public void ResumeDisplay()
    {
        _currentVideo?.Play();
        _emitter.GetComponent<AudioSource>().UnPause();
    }




    public void ChangeDisplay(DisplayState displayState)
    {
        _currentVideo?.Stop();
        _currentVideo = _stateVideoDic[displayState];
        _currentVideo?.Play();
        if (displayState == DisplayState.Off)
        {
            _renderer.material.color = Color.black;
            _renderer.material.DisableKeyword("_EMISSION");
            _emitter?.StopAndReturn();
            _emitter = null;
        }
        else
        {
            _renderer.material.color = Color.white;
            _renderer.material.EnableKeyword("_EMISSION");
            _emitter = SoundUtils.PlayOwnedScene3DSFX(_typingSD, transform.position, true, 1, true);
        }
    }



    private void OnDisable()
    {
        _emitter?.StopAndReturn();
    }

    private void OnDestroy()
    {
        _emitter?.StopAndReturn();
    }
}



public enum DisplayState
{
    Off, Working, Hacking, Gaming
}