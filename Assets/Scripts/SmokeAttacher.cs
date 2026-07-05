using UnityEngine;

public class SmokeAttacher : AnimAttacher
{
    [Header("Sockets")]
    [SerializeField] private Transform _packHandSocket;
    [SerializeField] private Transform _lighterHandSocket;
    [SerializeField] private Transform _cigaretteHandSocket;
    [SerializeField] private Transform _cigaretteMouthSocket;

    [Header("Props")]
    [SerializeField] private GameObject _cigarettePack;  // 담배갑
    [SerializeField] private GameObject _lighter;        // 라이터
    [SerializeField] private GameObject _cigarette;      // 담배 개비

    private Fire _smokeFire;



    private void Awake()
    {
        _smokeFire = _cigarette.GetComponentInChildren<Fire>();
    }



    public override void HideAll()
    {
        _smokeFire?.Extinguish();
        _cigarettePack.SetActive(false);
        _lighter.SetActive(false);
        _cigarette.SetActive(false);
    }

    // 1. 담배갑 꺼내기 (주머니 위치에서 손으로)
    public void ShowPack()
    {
        AttachProp(_cigarettePack, _packHandSocket);
        _cigarettePack.SetActive(true);
    }

    public void HidePack()
    {
        _cigarettePack.SetActive(false);
    }

    // 2. 담배 한 개비 입에 물기
    public void PutCigaretteInMouth()
    {
        AttachProp(_cigarette, _cigaretteMouthSocket);
        _cigarette.SetActive(true);
    }

    public void GrabCigarette()
    {
        AttachProp(_cigarette, _cigaretteHandSocket);
        _cigarette.SetActive(true);
    }

    public void ReleaseCigarette()
    {
        _smokeFire.Extinguish();
        _cigarette.SetActive(false);
    }

    public void ShowLighter()
    {
        AttachProp(_lighter, _lighterHandSocket);
        _lighter.SetActive(true);
    }

    public void HideLighter()
    {
        _lighter.SetActive(false);
    }


    public void IgniteCigarette()
    {
        _smokeFire.Ignite();
    }
}