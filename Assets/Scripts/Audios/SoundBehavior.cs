using UnityEngine;
using static SoundUtils;

public class SoundBehavior : MonoBehaviour
{
    [SerializeField] private SoundData _gruntSD;
    [SerializeField] private SoundData _goodSongSD;
    [SerializeField] private SoundData _badSongSD;
    [SerializeField] private SoundData _hurtSD;
    [SerializeField] private SoundData _sleepSD;
    [SerializeField] private SoundData _tackleSprintScreamSD;
    [SerializeField] private SoundData _rushChargeStartSD;

    private SoundEmitterOwner _soundEmitterOwner;

    private Transform _headBone;



    private void Awake()
    {
        _soundEmitterOwner = GetComponent<SoundEmitterOwner>();
        Animator animator = GetComponent<Animator>();
        _headBone = animator.GetBoneTransform(HumanBodyBones.Head);
    }



    public void PlayGrunt()
    {
        _soundEmitterOwner.Play3DSound(_gruntSD, _headBone, true);
    }



    public void PlayGoodSong()
    {
        _soundEmitterOwner.Play3DSound(
            _goodSongSD,
            _headBone,
            false,
            isLongDistance: true);
    }



    public void PlayBadSong()
    {
        _soundEmitterOwner.Play3DSound(
            _badSongSD,
            _headBone,
            false,
            isLongDistance: true);
    }



    public void PlayHurt()
    {
        _soundEmitterOwner.Play3DSound(_hurtSD, _headBone, false);
    }



    public void PlayTackleSprintScream()
    {
        if (_soundEmitterOwner == null || _headBone == null) return;
        _soundEmitterOwner.Play3DSound(_tackleSprintScreamSD, _headBone, true);
    }



    public void StopTackleSprintScream()
    {
        if (_soundEmitterOwner == null)
            _soundEmitterOwner = GetComponent<SoundEmitterOwner>();
        _soundEmitterOwner?.StopSound(_tackleSprintScreamSD);
    }



    public void PlayRushChargeStart()
    {
        if (_soundEmitterOwner == null || _headBone == null) return;
        _soundEmitterOwner.Play3DSound(
            _rushChargeStartSD,
            _headBone,
            true,
            volumeMultiplier: 1f,
            loop: false,
            isLongDistance: true);
    }



    public void StopRushChargeStart()
    {
        if (_soundEmitterOwner == null)
            _soundEmitterOwner = GetComponent<SoundEmitterOwner>();
        _soundEmitterOwner?.StopSound(_rushChargeStartSD);
    }


    public void StopSing()
    {
        if (_soundEmitterOwner == null)
            _soundEmitterOwner = GetComponent<SoundEmitterOwner>();
        _soundEmitterOwner.StopSound(_goodSongSD);
        _soundEmitterOwner.StopSound(_badSongSD);
    }



    public void PlaySleeping()
    {
        _soundEmitterOwner.Play3DSound(_sleepSD, _headBone, true, 1, true);
    }



    public void StopSleeping()
    {
        _soundEmitterOwner.StopSound(_sleepSD);
    }
}
