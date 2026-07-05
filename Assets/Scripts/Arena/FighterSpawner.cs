using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Utils;
using DG.Tweening;
using UnityEngine.AI;
using UnityEngine.Events;

public class FighterSpawner : MonoBehaviour
{
    [System.Serializable]
    public class FighterInfo
    {
        public Fighter mainComp;
        public bool isDead;
        public bool isBetted;
    }

    [SerializeField] private DamageData _fightData;
    [SerializeField] private Transform _startPoint1;
    [SerializeField] private Transform _startPoint2;
    [SerializeField] private Transform[] _spectatorSpots;
    private FighterInfo _fighter1;
    private FighterInfo _fighter2;
    private Transform _focusPoint;

    [Header("StudentInfo UIs")]
    [SerializeField] private RectTransform _leftPanel;
    [SerializeField] private Image _leftProfileImg;
    [SerializeField] private TextMeshProUGUI _leftNameTmp;
    [SerializeField] private StatBar _leftHealthBar;
    [SerializeField] private GameObject _leftBetPanel;
    [SerializeField] private TextMeshProUGUI _leftBetTmp;
    [SerializeField] private CanvasGroup _leftCanvasGroup;

    [SerializeField] private RectTransform _rightPanel;
    [SerializeField] private Image _rightProfileImg;
    [SerializeField] private TextMeshProUGUI _rightNameTmp;
    [SerializeField] private StatBar _rightHealthBar;
    [SerializeField] private GameObject _rightBetPanel;
    [SerializeField] private TextMeshProUGUI _rightBetTmp;
    [SerializeField] private CanvasGroup _rightCanvasGroup;
    [Header("MainPanel UIs")]
    //[SerializeField] private TextMeshProUGUI _timerTmp;
    [SerializeField] private BettingHelper _bettingHelper;
    [SerializeField] private FightFocusCamera _focusCamera;
    [SerializeField] private BetResultPanel _betResultPanel;
    [SerializeField] private LightMover _lightMover;
    [SerializeField] private Collider _groundCollider;
    [Header("Helmet & Gloves")]
    [SerializeField] private GameObject _leftGlovePrefab;
    [SerializeField] private GameObject _rightGlovePrefab;
    [SerializeField] private GameObject _helmetPrefab;
    [SerializeField] private Material _redMat;
    [SerializeField] private Material _blueMat;
    [Header("Sound Datas")]
    [SerializeField] private SoundData _matchStartSD;
    [SerializeField] private SoundData _matchEndSD;
    [SerializeField] private SoundData _crowdSD;
    [SerializeField] private SoundData _crowdApplauseSD;
    private bool _isFighting = false;
    private bool _isWinnerDetermined = false;
    private int _bettedMoney = 0;

    private SoundEmitter _crowdEmitter;

    public UnityEvent StartEvent = new();
    public UnityEvent EndEvent = new();



    private void Awake()
    {
        InventorySystem.Instance.ActivatePassiveItems();
        _fighter1 = new FighterInfo();
        _fighter2 = new FighterInfo();
    }



    private void Start()
    {
        int studentLayer = LayerMask.NameToLayer(Global.STUDENT_LAYER_NAME);
        Physics.IgnoreLayerCollision(studentLayer, studentLayer, false);
        _groundCollider.enabled = false;
        _betResultPanel.gameObject.SetActive(false);
        _leftBetPanel.SetActive(false);
        _rightBetPanel.SetActive(false);
        _focusPoint = new GameObject().transform;
        _focusPoint.position = (_startPoint1.transform.position + _startPoint2.transform.position) * 0.5f + Vector3.up;
        _focusCamera.target = _focusPoint;
        _lightMover.SetTarget(_focusPoint);
        SpawnFightersAndSpectators();
        AttachHelmetAndGloves();
        _fighter1.mainComp.DamageEvent.AddListener(OnFighterDamaged);
        _fighter2.mainComp.DamageEvent.AddListener(OnFighterDamaged);
        _fighter1.mainComp.DieEvent.AddListener(OnFighterDead);
        _fighter2.mainComp.DieEvent.AddListener(OnFighterDead);
    }


    private void Update()
    {
        if (_crowdEmitter != null && _crowdEmitter.gameObject.activeSelf)
        {
            _crowdEmitter.transform.position = _focusPoint.position;
        }

        if (_fighter1.isDead && _fighter2.isDead) return;
        Vector3 _focusPosition = Vector3.up; // 기본 높이 보정

        if (!_fighter1.isDead && !_fighter2.isDead)
        {
            // 둘 다 살아있을 때: 중간 지점 (0.5 + 0.5)
            _focusPosition += (_fighter1.mainComp.transform.position + _fighter2.mainComp.transform.position) * 0.5f;
        }
        else
        {
            // 한 명만 살아있을 때: 살아있는 사람의 위치를 100% 반영
            _focusPosition += _fighter1.isDead ? _fighter2.mainComp.transform.position : _fighter1.mainComp.transform.position;
            _focusPosition += Vector3.up * 0.5f;
        }

        _focusPoint.position = _focusPosition;
    }


    private void OnDrawGizmos()
    {
        if (_focusPoint == null) return;

        // 1. 점의 위치를 빨간 구체로 표시
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_focusPoint.position, 0.3f); // (위치, 반지름)

        // 2. 바닥으로부터의 높이를 알 수 있도록 선 그리기
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(_focusPoint.position, _focusPoint.position - Vector3.up * 2f);
    }



    public void OnFighterSelected(SelectedSide selectedSide)
    {
        _fighter1.mainComp.SetOutlines(selectedSide == SelectedSide.Left);
        _fighter2.mainComp.SetOutlines(selectedSide == SelectedSide.Right);
    }


    public void ChooseAndStartFight(SelectedSide selectedSide, int bettedMoney)
    {
        _isFighting = true;
        _bettedMoney = bettedMoney;
        _fighter1.mainComp.StartFight(_fighter2.mainComp.gameObject);
        _fighter2.mainComp.StartFight(_fighter1.mainComp.gameObject);
        FighterInfo choosedFighter = selectedSide == SelectedSide.Left ? _fighter1 : _fighter2;
        (selectedSide == SelectedSide.Left ? _leftBetPanel : _rightBetPanel).SetActive(true);
        (selectedSide == SelectedSide.Left ? _leftBetTmp : _rightBetTmp).text = $"${_bettedMoney.ToString("N0")}";
        _lightMover.OnFightStarted();
        _groundCollider.enabled = true;
        choosedFighter.isBetted = true;
        SoundUtils.PlayScene2DSFX(_matchStartSD);
        _crowdEmitter = SoundUtils.PlayOwnedScene3DSFX(_crowdSD, _focusPoint.position, false, 1, false);
        StartEvent?.Invoke();
    }



    private void OnFighterDamaged(Fighter fighter)
    {
        RectTransform targetPanel = fighter == _fighter1.mainComp ? _leftPanel : _rightPanel;
        targetPanel.DOShakeAnchorPos(0.5f, 20f, 10, 90f);
    }



    private void AttachHelmetAndGloves()
    {
        GameObject leftGlove1 = Instantiate(_leftGlovePrefab);
        GameObject rightGlove1 = Instantiate(_rightGlovePrefab);
        GameObject helmet1 = Instantiate(_helmetPrefab);

        GameObject leftGlove2 = Instantiate(_leftGlovePrefab);
        GameObject rightGlove2 = Instantiate(_rightGlovePrefab);
        GameObject helmet2 = Instantiate(_helmetPrefab);

        leftGlove1.GetComponentInChildren<Renderer>().material = _redMat;
        rightGlove1.GetComponentInChildren<Renderer>().material = _redMat;
        helmet1.GetComponent<Renderer>().material = _redMat;

        leftGlove2.GetComponentInChildren<Renderer>().material = _blueMat;
        rightGlove2.GetComponentInChildren<Renderer>().material = _blueMat;
        helmet2.GetComponent<Renderer>().material = _blueMat;

        _fighter1.mainComp.AttachLeftGlove(leftGlove1);
        _fighter1.mainComp.AttachRightGlove(rightGlove1);
        _fighter1.mainComp.AttachHelmet(helmet1);

        _fighter2.mainComp.AttachLeftGlove(leftGlove2);
        _fighter2.mainComp.AttachRightGlove(rightGlove2);
        _fighter2.mainComp.AttachHelmet(helmet2);
    }



    private void OnFighterDead(Fighter fighter)
    {
        RectTransform targetPanel = fighter == _fighter1.mainComp ? _leftPanel : _rightPanel;
        targetPanel.DOShakeAnchorPos(0.4f, 30f, 20);
        targetPanel.DOShakeRotation(0.4f, 10f);

        FighterInfo targetFighter = fighter == _fighter1.mainComp ? _fighter1 : _fighter2;
        targetFighter.isDead = true;

        CanvasGroup targetGroup = fighter == _fighter1.mainComp ? _leftCanvasGroup : _rightCanvasGroup;
        targetGroup.alpha = 0.2f;
        targetGroup.interactable = false;
        targetGroup.blocksRaycasts = false;

        _focusCamera.ZoomInToTarget();
        SoundUtils.PlayScene2DSFX(_matchEndSD);
        SoundUtils.PlayScene3DSFX(_crowdApplauseSD, _focusPoint.position);
        EndEvent?.Invoke();
        Invoke(nameof(DetermineWinner), 1f);
    }



    private void StopFighting()
    {
        if (_isFighting == false) return;
        _isFighting = false;
    }



    private void DetermineWinner()
    {
        if (_isWinnerDetermined) return;
        _isWinnerDetermined = true;
        CancelInvoke(nameof(DetermineWinner));

        int currentMoney = InventorySystem.Instance.Money;

        if (_fighter1.isDead == _fighter2.isDead)
        {
            //무승부
            _betResultPanel.Show(BetResult.None, currentMoney, 0);
        }
        else if (_fighter2.isDead)
        {
            //왼쪽 승리
            if (_fighter1.isBetted)
            {
                GainMoney();
                _betResultPanel.Show(BetResult.Success, currentMoney - _bettedMoney, _bettedMoney * 2);
            }
            else
            {
                LoseMoney();
                _betResultPanel.Show(BetResult.Failed, currentMoney - _bettedMoney, 0);
            }
        }
        else
        {
            //오른쪽 승리
            if (_fighter2.isBetted)
            {
                GainMoney();
                _betResultPanel.Show(BetResult.Success, currentMoney - _bettedMoney, _bettedMoney * 2);
            }
            else
            {
                LoseMoney();
                _betResultPanel.Show(BetResult.Failed, currentMoney - _bettedMoney, 0);
            }
        }
    }



    private void GainMoney()
    {
        int currentMoney = InventorySystem.Instance.Money;
        InventorySystem.Instance.SetMoney(currentMoney + _bettedMoney);
    }



    private void LoseMoney()
    {
        int currentMoney = InventorySystem.Instance.Money;
        InventorySystem.Instance.SetMoney(currentMoney - _bettedMoney);
    }



    private void SpawnFightersAndSpectators()
    {
        StudentEntry[] fighterEntries = StudentDB.Instance.GetRandomStudentEntries(2, out StudentEntry[] spectatorEntries);
        SpawnTwoFighters(fighterEntries[0], fighterEntries[1]);
        SpawnSpectators(spectatorEntries);
        BindFightersInfo(fighterEntries[0], fighterEntries[1]);
        _bettingHelper.WriteButtonNameTmp(fighterEntries[0].koreanName, fighterEntries[1].koreanName);
    }


    private void BindFightersInfo(StudentEntry leftFighterEntry, StudentEntry rightFighterEntry)
    {
        _leftProfileImg.sprite = leftFighterEntry.profile;
        _leftNameTmp.text = $"{leftFighterEntry.koreanName}  <size=60%>{leftFighterEntry.course}</size>";
        _leftHealthBar.SetTarget(_fighter1.mainComp.GetComponent<Health>());

        _rightProfileImg.sprite = rightFighterEntry.profile;
        _rightNameTmp.text = $"<size=60%>{rightFighterEntry.course}</size>  {rightFighterEntry.koreanName}";
        _rightHealthBar.SetTarget(_fighter2.mainComp.GetComponent<Health>());
    }



    public void SpawnTwoFighters(StudentEntry entry1, StudentEntry entry2)
    {
        _fighter1.mainComp = SpawnAndModifyToFighter(entry1.prefab, _startPoint1.position, _startPoint2.position);
        _fighter2.mainComp = SpawnAndModifyToFighter(entry2.prefab, _startPoint2.position, _startPoint1.position);
    }



    private void SpawnSpectators(StudentEntry[] spectatorEntries)
    {
        Transform[] spots = _spectatorSpots.GetRandomElements(spectatorEntries.Length);
        for (int i = 0; i < spots.Length; i++)
        {
            Vector3 lookDir = _focusPoint.position - spots[i].position;
            Spectator spectator = SpawnAndModifyToSpectator(spectatorEntries[i].prefab, spots[i].transform.position, lookDir);
            spectator.StartCheer(_focusPoint);
        }
    }



    public void SpawnTwoFighters(GameObject prefab1, GameObject prefab2)
    {
        _fighter1.mainComp = SpawnAndModifyToFighter(prefab1, _startPoint1.position, _startPoint2.position);
        _fighter2.mainComp = SpawnAndModifyToFighter(prefab2, _startPoint2.position, _startPoint1.position);
    }



    private Spectator SpawnAndModifyToSpectator(GameObject originalPrefab, Vector3 spawnPosition, Vector3 otherPosition)
    {
        bool originalState = originalPrefab.activeSelf;
        originalPrefab.SetActive(false);
        GameObject studentObj = Instantiate(originalPrefab, spawnPosition, Quaternion.LookRotation(otherPosition));
        originalPrefab.SetActive(originalState);
        studentObj.RemoveComponent<PostStudent>(true);
        studentObj.RemoveComponent<NavMeshAgent>(true);
        studentObj.RemoveComponent<DamageReceiver>(true);
        RemoveHealthCompsNotHundred(studentObj);
        studentObj.RemoveComponent<BaldOutlines>(true);
        studentObj.RemoveComponentsInChildren<Outline>(true, true);
        studentObj.RemoveComponentsInChildren<Fire>(true, true);
        studentObj.RemoveGameObjectsWithComponent<OverlapAttacker>(true, true);

        AnimAttacher[] animAttachers = studentObj.GetComponents<AnimAttacher>();
        foreach (AnimAttacher attacher in animAttachers)
        {
            attacher.HideAll();
        }

        studentObj.GetComponent<CharacterRagdoll>()._isAutoStandUp = false;
        studentObj.GetComponent<AnimAttack>()._damageData = _fightData;
        studentObj.AddComponent<DamageReceiver>();
        studentObj.AddComponent<Spectator>();

        studentObj.SetActive(true);
        return studentObj.GetComponent<Spectator>();
    }



    private Fighter SpawnAndModifyToFighter(GameObject originalPrefab, Vector3 spawnPosition, Vector3 otherPosition)
    {
        bool originalState = originalPrefab.activeSelf;
        originalPrefab.SetActive(false);
        GameObject studentObj = Instantiate(originalPrefab, spawnPosition, Quaternion.LookRotation(otherPosition));
        originalPrefab.SetActive(originalState);
        studentObj.RemoveComponent<PostStudent>(true);
        //studentObj.RemoveComponent<CharacterRagdoll>(true);
        studentObj.RemoveComponent<DamageReceiver>(true);
        RemoveHealthCompsNotHundred(studentObj);
        studentObj.RemoveComponent<BaldOutlines>(true);
        //studentObj.RemoveComponentsInChildren<Outline>(true, true);
        studentObj.RemoveComponentsInChildren<Fire>(true, true);
        studentObj.RemoveGameObjectsWithComponent<OverlapAttacker>(true, true);

        AnimAttacher[] animAttachers = studentObj.GetComponents<AnimAttacher>();
        foreach (AnimAttacher attacher in animAttachers)
        {
            attacher.HideAll();
        }

        studentObj.GetComponent<CharacterRagdoll>()._isAutoStandUp = false;
        studentObj.GetComponent<AnimAttack>()._damageData = _fightData;
        studentObj.AddComponent<DamageReceiver>();
        studentObj.AddComponent<Fighter>();

        studentObj.SetActive(true);
        return studentObj.GetComponent<Fighter>();
    }



    private void RemoveHealthCompsNotHundred(GameObject targetObject)
    {
        Health[] healths = targetObject.GetComponents<Health>();
        foreach (Health health in healths)
        {
            if (!Mathf.Approximately(health.Max, 100))
            {
                DestroyImmediate(health);
            }
        }
    }



    public void Store_Btn()
    {
        GameManager.Instance.GoStore();
    }


    public enum WinSide
    {
        None, Left, Right
    }



    private void OnDisable()
    {
        _crowdEmitter?.StopAndReturn();
    }



    private void OnDestroy()
    {
        _crowdEmitter?.StopAndReturn();
    }
}


public enum BetResult
{
    None, Failed, Success
}
