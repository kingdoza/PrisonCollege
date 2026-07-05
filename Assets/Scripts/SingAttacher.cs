using UnityEngine;

public class SingAttacher : AnimAttacher
{
    [Header("Normal")]
    [SerializeField] private Gender _gender;
    [SerializeField][Range(0f, 1f)] private float _badSongProbabiliy;
    [Header("Clips")]
    [SerializeField] private SongEntry _goodMaleSong;
    [SerializeField] private SongEntry _goodFemaleSong;
    [SerializeField] private SongEntry _badSong;

    [Header("Sockets")]
    [SerializeField] private Transform _handSocket;

    [Header("Props")]
    [SerializeField] private GameObject _microphone;

    private AudioSource _audioSource;
    private SongEntry _targetGoodSong;

    //public bool IsBad => _audioSource.clip != null && _audioSource.isPlaying && _audioSource.clip == _badSong.clip;
    public bool IsBad => _microphone.activeSelf && _isBadSongPlaying;
    private bool _isBadSongPlaying = false;



    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _targetGoodSong = _gender == Gender.Male ? _goodMaleSong : _goodFemaleSong;
    }



    public override void HideAll()
    {
        _isBadSongPlaying = false;
        GetComponent<SoundBehavior>().StopSing();
        _microphone.SetActive(false);
        if (_audioSource == null)
            _audioSource = GetComponent<AudioSource>();
        _audioSource.Stop();
        _audioSource.clip = null;
    }



    public void SingASong()
    {
        AttachProp(_microphone, _handSocket);
        _microphone.SetActive(true);

        float randValue = UnityEngine.Random.Range(0f, 1f);
        if (randValue < _badSongProbabiliy)
        {
            _isBadSongPlaying = true;
            GetComponent<SoundBehavior>().PlayBadSong();
        }
        else
        {
            _isBadSongPlaying = false;
            GetComponent<SoundBehavior>().PlayGoodSong();
        }
        //_audioSource.clip = randValue < _badSongProbabiliy ? _badSong.clip : _targetGoodSong.clip;
        //_audioSource.time = randValue < _badSongProbabiliy ? _badSong.startTime : _targetGoodSong.startTime;
        //_audioSource.volume = randValue < _badSongProbabiliy ? _badSong.volumeRate : _targetGoodSong.volumeRate;
        //_audioSource.Play();
    }
}


public enum Gender
{
    Male, Female
}



[System.Serializable]
public class SongEntry
{
    public AudioClip clip;
    [Range(0, 1)] public float volumeRate;
    public float startTime;
}