using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class MainMenuIntroPresenter : MonoBehaviour
{
    [Header("Video")]
    [Tooltip("인트로 영상과 스킵 UI를 모두 포함하는 최상위 오브젝트입니다.")]
    [SerializeField] private GameObject _overlayRoot;
    [Tooltip("인트로 재생 중 기존 메인 메뉴 UI 입력을 차단합니다.")]
    [SerializeField] private CanvasGroup _overlayCanvasGroup;
    [Tooltip("검은 배경은 제외하고 영상 RawImage만 포함하는 CanvasGroup입니다.")]
    [SerializeField] private CanvasGroup _videoCanvasGroup;
    [SerializeField] private VideoPlayer _videoPlayer;
    [Tooltip("인트로 재생 중 메인 메뉴 BGM을 임시 음소거하고 종료 후 복원하는 데 사용합니다.")]
    [SerializeField] private AudioSetting _audioSetting;
    [SerializeField, Min(0f)] private float _blackScreenDelay = 0.5f;
    [SerializeField, Min(0f)] private float _videoFadeInDuration = 0.5f;

    [Header("Hold To Skip")]
    [SerializeField] private GameObject _skipRoot;
    [Tooltip("Image Type을 Filled로 설정하십시오.")]
    [SerializeField] private Image _skipProgress;
    [Tooltip("등록된 키 중 하나라도 누르고 있으면 스킵 홀드가 진행됩니다.")]
    [SerializeField] private KeyCode[] _skipKeys = { KeyCode.Tab };
    [SerializeField, Min(0.1f)] private float _skipHoldDuration = 1.5f;

    private Action _completedAction;
    private Coroutine _startPlaybackCoroutine;
    private EscapeInputSystem _escapeInputSystem;
    private CursorLockMode _cursorLockModeBeforeIntro;
    private float _skipHoldTime;
    private bool _escapeInputWasEnabled;
    private bool _cursorWasVisibleBeforeIntro;
    private bool _hasCapturedCursorState;
    private bool _isFlowActive;
    private bool _isCompleting;
    private bool _didMuteBgm;



    private void Awake()
    {
        HideOverlay();
    }



    private void Update()
    {
        if (!_isFlowActive || _isCompleting || _videoPlayer == null || !_videoPlayer.isPlaying)
        {
            ResetSkipHold();
            return;
        }

        if (!IsAnySkipKeyHeld())
        {
            ResetSkipHold();
            return;
        }

        if (!_skipRoot.activeSelf)
            _skipRoot.SetActive(true);
        _skipHoldTime += Time.unscaledDeltaTime;
        UpdateSkipProgress();
        if (_skipHoldTime >= _skipHoldDuration)
            CompleteOnce();
    }



    public bool Play(Action completedAction)
    {
        if (_isFlowActive) return false;
        if (!ValidateReferences()) return false;

        _completedAction = completedAction;
        _isFlowActive = true;
        _isCompleting = false;
        ResetSkipHold();
        ShowOverlay();
        BlockEscapeInput();
        DisableCursor();
        MuteBgm();
        SubscribeVideoEvents();

        _videoPlayer.Stop();
        _videoPlayer.isLooping = false;
        _videoPlayer.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        _skipRoot.SetActive(false);
        _videoPlayer.Prepare();
        return true;
    }



    private bool ValidateReferences()
    {
        bool isValid = true;
        if (_overlayRoot == null)
        {
            Debug.LogError("MainMenuIntroPresenter의 Overlay Root 참조가 누락됐습니다.", this);
            isValid = false;
        }
        if (_overlayCanvasGroup == null)
        {
            Debug.LogError("MainMenuIntroPresenter의 Overlay Canvas Group 참조가 누락됐습니다.", this);
            isValid = false;
        }
        if (_videoCanvasGroup == null)
        {
            Debug.LogError("MainMenuIntroPresenter의 Video Canvas Group 참조가 누락됐습니다.", this);
            isValid = false;
        }
        if (_videoPlayer == null)
        {
            Debug.LogError("MainMenuIntroPresenter의 Video Player 참조가 누락됐습니다.", this);
            isValid = false;
        }
        if (_audioSetting == null)
        {
            Debug.LogError("MainMenuIntroPresenter의 Audio Setting 참조가 누락됐습니다.", this);
            isValid = false;
        }
        if (_skipRoot == null || _skipProgress == null)
        {
            Debug.LogError("MainMenuIntroPresenter의 홀드 스킵 UI 참조가 누락됐습니다.", this);
            isValid = false;
        }
        if (!HasValidSkipKey())
        {
            Debug.LogError("MainMenuIntroPresenter의 Skip Keys에 유효한 키를 하나 이상 설정해야 합니다.", this);
            isValid = false;
        }
        if (_videoPlayer != null)
        {
            bool hasSource = _videoPlayer.source == VideoSource.VideoClip
                ? _videoPlayer.clip != null
                : !string.IsNullOrWhiteSpace(_videoPlayer.url);
            if (!hasSource)
            {
                Debug.LogError("MainMenuIntroPresenter의 Video Player에 재생할 영상이 없습니다.", this);
                isValid = false;
            }
        }
        return isValid;
    }



    private void OnVideoPrepared(VideoPlayer source)
    {
        if (!_isFlowActive || _isCompleting || source != _videoPlayer) return;

        StopStartPlaybackCoroutine();
        _startPlaybackCoroutine = StartCoroutine(StartPlaybackRoutine(source));
    }



    private IEnumerator StartPlaybackRoutine(VideoPlayer source)
    {
        if (_blackScreenDelay > 0f)
            yield return new WaitForSecondsRealtime(_blackScreenDelay);

        if (!_isFlowActive || _isCompleting || source != _videoPlayer)
        {
            _startPlaybackCoroutine = null;
            yield break;
        }

        source.Play();
        if (_videoFadeInDuration <= 0f)
        {
            _videoCanvasGroup.alpha = 1f;
            _startPlaybackCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < _videoFadeInDuration)
        {
            if (!_isFlowActive || _isCompleting)
            {
                _startPlaybackCoroutine = null;
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            _videoCanvasGroup.alpha = Mathf.Clamp01(elapsed / _videoFadeInDuration);
            yield return null;
        }

        _videoCanvasGroup.alpha = 1f;
        _startPlaybackCoroutine = null;
    }



    private void OnVideoFinished(VideoPlayer source)
    {
        if (source == _videoPlayer)
            CompleteOnce();
    }



    private void OnVideoError(VideoPlayer source, string message)
    {
        if (source != _videoPlayer) return;

        Debug.LogError($"메인 메뉴 인트로 영상을 재생하지 못했습니다: {message}", this);
        CompleteOnce();
    }



    private void CompleteOnce()
    {
        if (!_isFlowActive || _isCompleting) return;

        _isCompleting = true;
        Action completedAction = _completedAction;
        CleanupPlayback(true);
        _isFlowActive = false;
        _isCompleting = false;
        _completedAction = null;
        completedAction?.Invoke();
    }



    private void SubscribeVideoEvents()
    {
        UnsubscribeVideoEvents();
        _videoPlayer.prepareCompleted += OnVideoPrepared;
        _videoPlayer.loopPointReached += OnVideoFinished;
        _videoPlayer.errorReceived += OnVideoError;
    }



    private void UnsubscribeVideoEvents()
    {
        if (_videoPlayer == null) return;

        _videoPlayer.prepareCompleted -= OnVideoPrepared;
        _videoPlayer.loopPointReached -= OnVideoFinished;
        _videoPlayer.errorReceived -= OnVideoError;
    }



    private void ShowOverlay()
    {
        _videoCanvasGroup.alpha = 0f;
        _overlayRoot.SetActive(true);
        _overlayCanvasGroup.alpha = 1f;
        _overlayCanvasGroup.interactable = true;
        _overlayCanvasGroup.blocksRaycasts = true;
    }



    private void HideOverlay()
    {
        if (_overlayCanvasGroup != null)
        {
            _overlayCanvasGroup.alpha = 0f;
            _overlayCanvasGroup.interactable = false;
            _overlayCanvasGroup.blocksRaycasts = false;
        }
        if (_videoCanvasGroup != null)
            _videoCanvasGroup.alpha = 0f;
        if (_skipRoot != null)
            _skipRoot.SetActive(false);
        if (_overlayRoot != null)
            _overlayRoot.SetActive(false);
    }



    private void UpdateSkipProgress()
    {
        if (_skipProgress != null)
            _skipProgress.fillAmount = Mathf.Clamp01(_skipHoldTime / _skipHoldDuration);
    }



    private bool IsAnySkipKeyHeld()
    {
        if (_skipKeys == null) return false;

        for (int i = 0; i < _skipKeys.Length; i++)
        {
            KeyCode key = _skipKeys[i];
            if (key != KeyCode.None && Input.GetKey(key))
                return true;
        }
        return false;
    }



    private bool HasValidSkipKey()
    {
        if (_skipKeys == null) return false;

        for (int i = 0; i < _skipKeys.Length; i++)
        {
            if (_skipKeys[i] != KeyCode.None)
                return true;
        }
        return false;
    }



    private void ResetSkipHold()
    {
        bool hasProgress = _skipHoldTime > 0f
            || (_skipProgress != null && _skipProgress.fillAmount > 0f);
        if (!hasProgress && (_skipRoot == null || !_skipRoot.activeSelf))
            return;

        _skipHoldTime = 0f;
        if (_skipProgress != null)
            _skipProgress.fillAmount = 0f;
        if (_skipRoot != null)
            _skipRoot.SetActive(false);
    }



    private void BlockEscapeInput()
    {
        _escapeInputSystem = EscapeInputSystem.Instance;
        if (_escapeInputSystem == null) return;

        _escapeInputWasEnabled = _escapeInputSystem.enabled;
        _escapeInputSystem.enabled = false;
    }



    private void RestoreEscapeInput()
    {
        if (_escapeInputSystem != null)
            _escapeInputSystem.enabled = _escapeInputWasEnabled;
        _escapeInputSystem = null;
    }



    private void DisableCursor()
    {
        if (_hasCapturedCursorState) return;

        _cursorWasVisibleBeforeIntro = Cursor.visible;
        _cursorLockModeBeforeIntro = Cursor.lockState;
        _hasCapturedCursorState = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }



    private void RestoreCursor()
    {
        if (!_hasCapturedCursorState) return;

        Cursor.lockState = _cursorLockModeBeforeIntro;
        Cursor.visible = _cursorWasVisibleBeforeIntro;
        _hasCapturedCursorState = false;
    }



    private void MuteBgm()
    {
        _audioSetting.MuteBGM();
        _didMuteBgm = true;
    }



    private void RestoreBgm(bool playNextBgm)
    {
        if (!_didMuteBgm || _audioSetting == null) return;

        if (playNextBgm)
        {
            GameManager gameManager = GameManager.Instance;
            if (gameManager != null)
                gameManager.PlayNextBGM();
        }
        _audioSetting.ApplyVolumes();
        _didMuteBgm = false;
    }



    private void CleanupPlayback(bool playNextBgm)
    {
        StopStartPlaybackCoroutine();
        UnsubscribeVideoEvents();
        if (_videoPlayer != null)
            _videoPlayer.Stop();
        ResetSkipHold();
        RestoreEscapeInput();
        RestoreCursor();
        RestoreBgm(playNextBgm);
        HideOverlay();
    }



    private void StopStartPlaybackCoroutine()
    {
        if (_startPlaybackCoroutine == null) return;

        StopCoroutine(_startPlaybackCoroutine);
        _startPlaybackCoroutine = null;
    }



    private void OnDisable()
    {
        if (!_isFlowActive || _isCompleting) return;

        CleanupPlayback(false);
        _isFlowActive = false;
        _completedAction = null;
    }



    private void OnDestroy()
    {
        UnsubscribeVideoEvents();
        RestoreEscapeInput();
        RestoreCursor();
        RestoreBgm(false);
    }
}
