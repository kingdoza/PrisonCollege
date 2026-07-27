using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TutorialActorDirector : MonoBehaviour
{
    [Header("Required scene references")]
    [SerializeField] private StageController _stageController;
    [SerializeField] private Professor _player;
    [SerializeField] private StageSpots _stageSpots;
    [SerializeField] private Transform _studentParent;
    [Tooltip("StudentDB 순서와 1:1로 대응하는 고정 대기 슬롯입니다.")]
    [SerializeField] private Transform[] _waitingSlots = Array.Empty<Transform>();
    [SerializeField] private Transform _trainingEntryNavPoint;
    [SerializeField] private Transform _waitingReturnNavPoint;

    [Header("Behavior runtime")]
    [Tooltip("0단계 전 전체 학생 runtime 초기화에 사용합니다. P-29 미니웨이브 asset과 별개입니다.")]
    [SerializeField] private BehaviorWeightSet _trainingBehaviorWeightSet;
    [SerializeField] private float _trainingMoveSpeed = PostStudent._fastRunSpeed;

    [Header("Animator bool names")]
    [Tooltip("Loyalty/Standby 자세용 bool입니다. 빈 값이면 기존 MoveSpeed 0 Idle을 사용합니다.")]
    [SerializeField] private string _loyaltyPoseBool;
    [SerializeField] private string _standbyPoseBool;

    [Header("Step 3 - P-09/P-10")]
    [Tooltip("정상 4명/위험 4명 총 8개를 연결합니다. spot은 씬의 고정 BehaveSpot입니다.")]
    [SerializeField] private TutorialRiskRoleBinding[] _riskRoles = Array.Empty<TutorialRiskRoleBinding>();

    [Header("Step 3-3 - 전자레인지 관리")]
    [Tooltip("음식 학생을 일렬 배치할 고정 슬롯입니다. StudentDB index 0 PlateAttacher의 정상/위험 음식 총수 이상 연결합니다.")]
    [SerializeField] private Transform[] _microwaveFoodDisplaySlots = Array.Empty<Transform>();

    [Header("Step 4-1")]
    [SerializeField] private BehaveSpot _innocentTrainingSpot;
    [SerializeField] private BehaviorType _innocentTrainingBehavior = BehaviorType.LookAround;

    [Header("Step 6")]
    [Tooltip("6단계 대상 학생이 부스터 작업 효과를 기다릴 위치입니다. NavMesh 위의 Transform을 연결합니다.")]
    [SerializeField] private Transform _studentWorkBoostWaitingPoint;
    [Tooltip("부스터 작업 효과가 발동된 뒤 학생이 이동해 실제로 작업할 Work spot입니다.")]
    [SerializeField] private BehaveSpot _studentWorkTrainingSpot;

    [Header("Mini wave")]
    [Tooltip("미니웨이브 선발 학생 순서와 1:1로 대응할 컴퓨터 지정석입니다. 서로 다른 Work 지원 MonitorSpot을 학생 수 이상 연결합니다.")]
    [SerializeField] private MonitorSpot[] _miniWaveComputerSeats = Array.Empty<MonitorSpot>();

    private readonly List<PostStudent> _students = new();
    private readonly List<PostStudent> _riskStudents = new();
    private readonly List<PostStudent> _riskHazardStudents = new();
    private readonly List<PostStudent> _microwaveDisplayStudents = new();
    private readonly List<PostStudent> _miniWaveStudents = new();
    private readonly Dictionary<PostStudent, int> _slotByStudent = new();
    private readonly Dictionary<PostStudent, Transform> _bubbleAnchorByStudent = new();
    private readonly Dictionary<PostStudent, Coroutine> _movementByStudent = new();
    private readonly HashSet<PostStudent> _returnAfterStandUp = new();
    private PostStudent _innocentStudent;
    private PostStudent _studentWorkStudent;
    private PlateAttacher _foodCatalogSource;
    private BehaviorWeightSet _preparedMiniWaveWeights;
    private bool _isMiniWavePrepared;
    private bool _isInitialized;

    public IReadOnlyList<PostStudent> Students => _students;
    public IReadOnlyList<PostStudent> RiskHazardStudents => _riskHazardStudents;
    public IReadOnlyList<PostStudent> MiniWaveStudents => _miniWaveStudents;
    public PostStudent InnocentStudent => _innocentStudent;
    public PostStudent StudentWorkStudent => _studentWorkStudent;
    public bool HasPreparedMiniWave => _isMiniWavePrepared
        && _preparedMiniWaveWeights != null
        && _miniWaveStudents.Count > 0;
    public bool IsInitialized => _isInitialized;
    public event Action<PostStudent> TrainingDestinationReached;

    public bool TryGetBubbleAnchor(PostStudent student, out Transform anchorBone)
    {
        if (student != null
            && _bubbleAnchorByStudent.TryGetValue(student, out anchorBone)
            && anchorBone != null)
            return true;

        anchorBone = null;
        return false;
    }



    public bool InitializePool()
    {
        if (_isInitialized) return true;
        if (!ValidateRequiredReferences()) return false;

        StudentEntry[] entries = StudentDB.Instance.GetAllStudentEntries();
        if (entries.Length < 9)
        {
            Debug.LogError($"3단계 8명과 4-1 잔여 1명을 위해 최소 9명이 필요하지만 StudentDB에는 {entries.Length}명만 있습니다.", this);
            return false;
        }
        if (_waitingSlots.Length < entries.Length)
        {
            Debug.LogError($"대기 슬롯 {_waitingSlots.Length}개로 StudentDB {entries.Length}명을 배치할 수 없습니다.", this);
            return false;
        }

        TutorialBehaviorRuntimeContext context = new(
            _player,
            _stageSpots,
            _trainingBehaviorWeightSet);

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].prefab == null || _waitingSlots[i] == null)
            {
                Debug.LogError($"StudentDB index {i} prefab 또는 대기 슬롯 참조가 없습니다.", this);
                return false;
            }

            Transform slot = _waitingSlots[i];
            GameObject spawned = Instantiate(
                entries[i].prefab,
                slot.position,
                slot.rotation,
                _studentParent);
            PostStudent student = spawned.GetComponent<PostStudent>();
            if (student == null)
            {
                Debug.LogError($"StudentDB index {i} prefab에 PostStudent가 없습니다.", entries[i].prefab);
                return false;
            }

            EnforceableDamageReceiver damageReceiver = spawned.GetComponent<EnforceableDamageReceiver>();
            if (damageReceiver == null)
            {
                Debug.LogError($"StudentDB index {i} prefab에 EnforceableDamageReceiver가 없습니다.", entries[i].prefab);
                return false;
            }
            damageReceiver.SetEnforced(false);

            if (i == 0)
            {
                _foodCatalogSource = spawned.GetComponent<PlateAttacher>();
                if (_foodCatalogSource == null
                    || !_foodCatalogSource.TryGetFoodCatalogSnapshot(out _))
                {
                    Debug.LogError("StudentDB index 0 prefab의 PlateAttacher 정상/위험 음식 목록이 비었거나 유효하지 않습니다.", entries[i].prefab);
                    return false;
                }
            }

            Animator animator = student.GetComponent<Animator>();
            Transform bubbleAnchor = null;
            if (animator != null && animator.avatar != null && animator.avatar.isHuman)
            {
                bubbleAnchor = animator.GetBoneTransform(HumanBodyBones.UpperChest);
                if (bubbleAnchor == null)
                    bubbleAnchor = animator.GetBoneTransform(HumanBodyBones.Chest);
            }
            if (bubbleAnchor == null)
            {
                Debug.LogError($"StudentDB index {i} prefab의 Humanoid UpperChest 또는 Chest 본을 찾을 수 없습니다.", entries[i].prefab);
                return false;
            }

            student.name = $"TutorialStudent_{entries[i].id}";
            student.Name = entries[i].koreanName;
            student.BehaviorWeightSet = _trainingBehaviorWeightSet;
            if (!student.InitializeTutorialBehaviorRuntime(context))
                return false;

            student.TutorialStandUpCompletedEvent += OnTutorialStudentStandUpCompleted;
            _students.Add(student);
            _slotByStudent[student] = i;
            _bubbleAnchorByStudent[student] = bubbleAnchor;
            _stageController.RegisterStudent(student);
        }

        // 모든 학생의 동기 초기화 호출이 반환된 뒤에만 true가 된다.
        _isInitialized = true;
        SetAllLoyalty();
        return true;
    }



    public void SetAllLoyalty()
    {
        foreach (PostStudent student in _students)
        {
            CancelMovement(student);
            ReturnToSlotImmediate(student, TutorialStudentMode.Loyalty, _loyaltyPoseBool);
        }
    }



    public void SetAllStandby()
    {
        foreach (PostStudent student in _students)
        {
            if (student.TutorialMode == TutorialStudentMode.ReturnTransit) continue;
            CancelMovement(student);
            ReturnToSlotImmediate(student, TutorialStudentMode.Standby, _standbyPoseBool);
        }
    }



    public bool BeginRiskTraining()
    {
        if (!_isInitialized || !ValidateRiskRoles()) return false;
        _riskStudents.Clear();
        _riskHazardStudents.Clear();

        List<PostStudent> shuffled = new(_students);
        Shuffle(shuffled);
        _innocentStudent = shuffled[8];

        for (int i = 0; i < 8; i++)
        {
            PostStudent student = shuffled[i];
            TutorialRiskRoleBinding role = _riskRoles[i];
            _riskStudents.Add(student);
            if (role.isHazard) _riskHazardStudents.Add(student);
            student.SetTutorialAutoStandUp(false);

            ScriptedBehaviorRequest request = new()
            {
                scenarioId = $"risk_{i}_{role.behavior}",
                behavior = role.behavior,
                fixedSpot = role.spot,
                holdUntilResolved = true,
                // 3단계 위험 행동은 연출만 유지하고 조명, 탈출, 화재/침수 등 월드 결과를 만들지 않는다.
                suppressWorldConsequences = role.isHazard,
                suppressOutgoingDamage = true,
                overrideSongQuality = role.behavior == BehaviorType.Sing,
                useBadSong = role.behavior == BehaviorType.Sing && role.isHazard,
            };
            BeginTrainingTransit(student, request);
        }

        return true;
    }



    public void EndRiskTraining()
    {
        foreach (PostStudent student in _riskStudents)
        {
            student.RestoreTutorialAutoStandUp();
            if (student.IsHealthDepleted)
            {
                _returnAfterStandUp.Add(student);
            }
            else
            {
                BeginReturn(student);
            }
        }
    }



    public bool BeginMicrowaveFoodDisplay(IReadOnlyList<GameObject> foodSamples)
    {
        if (!_isInitialized || foodSamples == null || foodSamples.Count == 0)
        {
            Debug.LogError("3-3 음식 샘플이 없거나 학생 풀이 초기화되지 않았습니다.", this);
            return false;
        }
        if (_microwaveFoodDisplaySlots == null
            || _microwaveFoodDisplaySlots.Length < foodSamples.Count)
        {
            Debug.LogError($"3-3 음식 샘플 {foodSamples.Count}개를 배치할 슬롯이 부족합니다.", this);
            return false;
        }

        for (int i = 0; i < foodSamples.Count; i++)
        {
            if (foodSamples[i] == null || _microwaveFoodDisplaySlots[i] == null)
            {
                Debug.LogError($"3-3 foodSamples[{i}] 또는 display slot 참조가 없습니다.", this);
                return false;
            }
        }

        List<PostStudent> candidates = new();
        foreach (PostStudent student in _students)
        {
            if (student != null && student.gameObject.activeInHierarchy && !student.IsHealthDepleted)
                candidates.Add(student);
        }
        if (candidates.Count < foodSamples.Count)
        {
            Debug.LogError($"3-3 음식 학생 {foodSamples.Count}명이 필요하지만 현재 배치 가능한 학생은 {candidates.Count}명입니다.", this);
            return false;
        }

        Shuffle(candidates);
        _microwaveDisplayStudents.Clear();
        for (int i = 0; i < foodSamples.Count; i++)
        {
            PostStudent student = candidates[i];
            Transform slot = _microwaveFoodDisplaySlots[i];
            CancelMovement(student);
            _returnAfterStandUp.Remove(student);
            student.SetTutorialBoostBlocked(true);
            student.SetTutorialMode(TutorialStudentMode.Standby);
            student.ApplyTutorialPose(null);
            if (!student.ShowTutorialFood(foodSamples[i]))
            {
                Debug.LogError($"[{student.name}] 3-3 음식 소품을 표시하지 못했습니다.", student);
                EndMicrowaveFoodDisplay();
                return false;
            }
            _microwaveDisplayStudents.Add(student);
            student.SetTutorialMode(TutorialStudentMode.TrainingTransit);
            student.WarpForTutorial(_trainingEntryNavPoint.position, _trainingEntryNavPoint.rotation);
            if (!student.MoveForTutorial(slot.position, _trainingMoveSpeed))
            {
                Debug.LogError($"[{student.name}] 3-3 음식 시연 슬롯까지 NavMesh 이동을 시작하지 못했습니다.", student);
                EndMicrowaveFoodDisplay();
                return false;
            }
            _movementByStudent[student] = StartCoroutine(
                MicrowaveFoodTransitRoutine(student, slot.rotation));
        }
        return true;
    }



    private IEnumerator MicrowaveFoodTransitRoutine(PostStudent student, Quaternion finalRotation)
    {
        NavMeshAgent agent = student.TutorialAgent;
        while (agent != null
            && agent.enabled
            && agent.isOnNavMesh
            && (agent.pathPending || agent.remainingDistance > agent.stoppingDistance))
        {
            yield return null;
        }

        _movementByStudent.Remove(student);
        student.StopTutorialMovementAnimation();
        student.transform.rotation = finalRotation;
        student.SetTutorialMode(TutorialStudentMode.Standby);
    }



    public bool TryGetMicrowaveFoodCatalog(out FoodInfo[] foodSamples)
    {
        foodSamples = Array.Empty<FoodInfo>();
        if (!_isInitialized
            || _foodCatalogSource == null
            || !_foodCatalogSource.TryGetFoodCatalogSnapshot(out FoodInfo[] snapshot))
        {
            Debug.LogError("3-3에서 사용할 StudentDB index 0 PlateAttacher 음식 목록을 읽지 못했습니다.", this);
            return false;
        }

        foodSamples = snapshot;
        return true;
    }



    public void EndMicrowaveFoodDisplay()
    {
        foreach (PostStudent student in _microwaveDisplayStudents)
        {
            if (student == null) continue;
            student.ClearTutorialFood();
            BeginReturn(student);
        }
        _microwaveDisplayStudents.Clear();
    }



    public bool BeginInnocentStudentTraining()
    {
        if (_innocentStudent == null || _innocentTrainingSpot == null)
        {
            Debug.LogError("4-1 대상 학생 또는 innocentTrainingSpot 참조가 없습니다.", this);
            return false;
        }

        ScriptedBehaviorRequest request = new()
        {
            scenarioId = "innocent_student",
            behavior = _innocentTrainingBehavior,
            fixedSpot = _innocentTrainingSpot,
            holdUntilResolved = true,
        };
        BeginTrainingTransit(_innocentStudent, request);
        return true;
    }



    public void EndInnocentStudentTraining()
    {
        if (_innocentStudent == null) return;
        if (_innocentStudent.IsHealthDepleted)
            _returnAfterStandUp.Add(_innocentStudent);
        else
            BeginReturn(_innocentStudent);
    }



    public bool BeginStudentWorkTraining()
    {
        if (_studentWorkBoostWaitingPoint == null || _studentWorkTrainingSpot == null)
        {
            Debug.LogError("6단계 부스터 대기 위치 또는 studentWorkTrainingSpot 참조가 없습니다.", this);
            return false;
        }

        _studentWorkStudent = null;
        foreach (PostStudent student in _students)
        {
            if (student == _innocentStudent) continue;
            if (student.TutorialMode != TutorialStudentMode.Standby) continue;
            _studentWorkStudent = student;
            break;
        }
        if (_studentWorkStudent == null)
        {
            Debug.LogError("6단계에 사용할 복귀 완료 Standby 학생이 없습니다.", this);
            return false;
        }

        BeginTrainingTransit(
            _studentWorkStudent,
            null,
            _studentWorkBoostWaitingPoint.position,
            _studentWorkTrainingSpot,
            _studentWorkBoostWaitingPoint.rotation);
        return true;
    }



    public void EndStudentWorkTraining()
    {
        if (_studentWorkStudent == null) return;
        _studentWorkStudent.ForceStopTutorialWork();
        BeginReturn(_studentWorkStudent);
    }



    public bool PrepareMiniWaveRoster(
        int count,
        BehaviorWeightSet miniWaveWeights,
        IReadOnlyList<PostStudent> fixedRoster = null)
    {
        if (!_isInitialized || miniWaveWeights == null)
        {
            Debug.LogError("P-29 미니웨이브 BehaviorWeightSet 참조가 없습니다.", this);
            return false;
        }
        if (count <= 0 || count > _students.Count)
        {
            Debug.LogError($"미니웨이브 학생 수 {count}가 전체 학생 수 {_students.Count} 범위를 벗어났습니다.", this);
            return false;
        }

        List<PostStudent> selected = new(count);
        if (fixedRoster != null)
        {
            if (fixedRoster.Count != count)
            {
                Debug.LogError($"고정 미니웨이브 로스터 수 {fixedRoster.Count}가 요청 수 {count}와 다릅니다.", this);
                return false;
            }
            HashSet<PostStudent> unique = new();
            for (int i = 0; i < fixedRoster.Count; i++)
            {
                PostStudent student = fixedRoster[i];
                if (student == null || !_students.Contains(student) || !unique.Add(student))
                {
                    Debug.LogError("고정 미니웨이브 로스터에 null, 중복 또는 풀 외 학생이 있습니다.", this);
                    return false;
                }
                selected.Add(student);
            }
        }
        else
        {
            List<PostStudent> shuffled = new(_students);
            Shuffle(shuffled);
            selected.AddRange(shuffled.GetRange(0, count));
        }

        if (!ValidateMiniWaveComputerSeats(count))
            return false;

        StopAllMovement();
        foreach (PostStudent student in _students)
        {
            student.SetTutorialBoostBlocked(true);
            ReturnToSlotImmediate(student, TutorialStudentMode.Standby, _standbyPoseBool);
            student.SeatSpot = null;
        }

        for (int i = 0; i < selected.Count; i++)
            selected[i].SeatSpot = _miniWaveComputerSeats[i];

        _miniWaveStudents.Clear();
        _miniWaveStudents.AddRange(selected);
        _preparedMiniWaveWeights = miniWaveWeights;
        _isMiniWavePrepared = true;
        return true;
    }



    public bool StartPreparedMiniWave()
    {
        if (!HasPreparedMiniWave)
        {
            Debug.LogError("준비된 미니웨이브 로스터 또는 BehaviorWeightSet이 없습니다.", this);
            return false;
        }
        if (!ValidatePreparedMiniWaveComputerSeats())
            return false;

        foreach (PostStudent student in _students)
        {
            CancelMovement(student);
            student.SetTutorialBoostBlocked(false);
            if (_miniWaveStudents.Contains(student))
            {
                student.WarpForTutorial(_trainingEntryNavPoint.position, _trainingEntryNavPoint.rotation);
                student.StartTutorialMiniWave(_preparedMiniWaveWeights);
            }
            else
            {
                ReturnToSlotImmediate(student, TutorialStudentMode.Cheer, null);
            }
        }
        return true;
    }



    public bool StartMiniWave(int count, BehaviorWeightSet miniWaveWeights, IReadOnlyList<PostStudent> fixedRoster = null)
    {
        return PrepareMiniWaveRoster(count, miniWaveWeights, fixedRoster)
            && StartPreparedMiniWave();
    }



    public TutorialActorPoolSnapshot CaptureSnapshot()
    {
        TutorialActorPoolSnapshot snapshot = new();
        snapshot.miniWaveRoster.AddRange(_miniWaveStudents);
        foreach (PostStudent student in _miniWaveStudents)
            snapshot.miniWaveComputerSeats.Add(student.SeatSpot);
        foreach (PostStudent student in _students)
            snapshot.studentStates.Add(student.CaptureTutorialResetState());
        return snapshot;
    }



    public bool RestoreSnapshot(TutorialActorPoolSnapshot snapshot)
    {
        if (snapshot == null
            || snapshot.studentStates.Count != _students.Count
            || snapshot.miniWaveRoster.Count != snapshot.miniWaveComputerSeats.Count)
            return false;
        StopAllMovement();

        foreach (PostStudent student in _students)
            student.SeatSpot = null;
        for (int i = 0; i < snapshot.miniWaveRoster.Count; i++)
        {
            PostStudent student = snapshot.miniWaveRoster[i];
            MonitorSpot seat = snapshot.miniWaveComputerSeats[i];
            if (student == null || !_students.Contains(student) || seat == null)
            {
                Debug.LogError("미니웨이브 체크포인트의 학생 또는 컴퓨터 지정석 참조가 유효하지 않습니다.", this);
                return false;
            }
            student.SeatSpot = seat;
        }

        _miniWaveStudents.Clear();
        _miniWaveStudents.AddRange(snapshot.miniWaveRoster);
        _isMiniWavePrepared = _preparedMiniWaveWeights != null && _miniWaveStudents.Count > 0;
        for (int i = 0; i < _students.Count; i++)
        {
            if (!_students[i].ResetTutorialBehaviorRuntime(snapshot.studentStates[i]))
                return false;
            if (_students[i].TutorialMode == TutorialStudentMode.Cheer
                && !_students[i].IsHealthDepleted)
                _students[i].StartTutorialCheer();
        }
        return true;
    }



    public void StopAllActors()
    {
        StopAllMovement();
        foreach (PostStudent student in _students)
        {
            if (student == null) continue;
            student.ClearTutorialFood();
            student.SetTutorialBoostBlocked(true);
            if (!student.gameObject.activeInHierarchy) continue;
            student.SetTutorialMode(TutorialStudentMode.Cheer);
            if (!student.IsHealthDepleted)
                student.StartTutorialCheer();
        }
    }



    public void SetAllBoostBlocked(bool isBlocked)
    {
        foreach (PostStudent student in _students)
            student.SetTutorialBoostBlocked(isBlocked);
    }



    public void ClearMiniWaveComputerSeats()
    {
        foreach (PostStudent student in _students)
            if (student != null) student.SeatSpot = null;
    }



    private void BeginTrainingTransit(
        PostStudent student,
        ScriptedBehaviorRequest? request,
        Vector3? destinationOverride = null,
        BehaveSpot boostedWorkSpot = null,
        Quaternion? arrivalRotation = null)
    {
        CancelMovement(student);
        student.SetTutorialMode(TutorialStudentMode.TrainingTransit);
        student.ApplyTutorialPose(null);
        student.WarpForTutorial(_trainingEntryNavPoint.position, _trainingEntryNavPoint.rotation);
        Vector3 destination = destinationOverride ?? request.Value.fixedSpot.transform.position;
        if (!student.MoveForTutorial(destination, _trainingMoveSpeed))
            Debug.LogError($"[{student.name}] 연수 지점 NavMesh 이동을 시작하지 못했습니다.", student);
        _movementByStudent[student] = StartCoroutine(
            TrainingTransitRoutine(student, request, boostedWorkSpot, arrivalRotation));
    }



    private IEnumerator TrainingTransitRoutine(
        PostStudent student,
        ScriptedBehaviorRequest? request,
        BehaveSpot boostedWorkSpot,
        Quaternion? arrivalRotation)
    {
        NavMeshAgent agent = student.TutorialAgent;
        while (agent.enabled
            && (agent.pathPending || agent.remainingDistance > agent.stoppingDistance))
        {
            yield return null;
        }

        _movementByStudent.Remove(student);
        student.StopTutorialMovementAnimation();
        if (arrivalRotation.HasValue)
            student.transform.rotation = arrivalRotation.Value;
        student.SetTutorialMode(TutorialStudentMode.Training);
        TrainingDestinationReached?.Invoke(student);
        if (request.HasValue)
            student.BeginScriptedBehavior(request.Value);
        else if (boostedWorkSpot != null
            && !student.PrepareTutorialBoostedWorkSpot(boostedWorkSpot))
            Debug.LogError($"[{student.name}] 6단계 Work spot을 예약하지 못했습니다.", student);
    }



    private void BeginReturn(PostStudent student)
    {
        if (student == null || !isActiveAndEnabled || !gameObject.activeInHierarchy)
            return;

        CancelMovement(student);
        student.SetTutorialMode(TutorialStudentMode.ReturnTransit);
        if (student.IsHealthDepleted)
        {
            _returnAfterStandUp.Add(student);
            return;
        }

        Vector3 returnPosition = _waitingReturnNavPoint.position;
        if (!student.TryRecoverTutorialAgentOnNavMesh(returnPosition))
        {
            Debug.LogError($"[{student.name}] 복귀 전 NavMesh 위치를 복구하지 못해 대기 슬롯으로 즉시 이동합니다.", student);
            ReturnToSlotImmediate(student, TutorialStudentMode.Standby, _standbyPoseBool);
            return;
        }
        if (!student.MoveForTutorial(returnPosition, _trainingMoveSpeed))
        {
            Debug.LogError($"[{student.name}] 복귀 NavMesh 이동을 시작하지 못했습니다.", student);
            ReturnToSlotImmediate(student, TutorialStudentMode.Standby, _standbyPoseBool);
            return;
        }
        _movementByStudent[student] = StartCoroutine(ReturnTransitRoutine(student));
    }



    private IEnumerator ReturnTransitRoutine(PostStudent student)
    {
        NavMeshAgent agent = student.TutorialAgent;
        while (agent != null
            && agent.enabled
            && agent.isOnNavMesh
            && (agent.pathPending || agent.remainingDistance > agent.stoppingDistance))
        {
            yield return null;
        }

        _movementByStudent.Remove(student);
        student.StopTutorialMovementAnimation();
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            ReturnToSlotImmediate(student, TutorialStudentMode.Standby, _standbyPoseBool);
            yield break;
        }
        ReturnToSlotImmediate(student, TutorialStudentMode.Standby, _standbyPoseBool);
    }



    private void OnTutorialStudentStandUpCompleted(PostStudent student)
    {
        if (!_returnAfterStandUp.Remove(student)) return;
        BeginReturn(student);
    }



    private void ReturnToSlotImmediate(PostStudent student, TutorialStudentMode mode, string poseBool)
    {
        int slotIndex = _slotByStudent[student];
        Transform slot = _waitingSlots[slotIndex];
        if (!student.WarpForTutorial(slot.position, slot.rotation))
            Debug.LogError($"[{student.name}] 대기 슬롯 주변 1m 안에 NavMesh가 없습니다.", slot);
        student.SetTutorialMode(mode);
        if (mode == TutorialStudentMode.Cheer)
            student.StartTutorialCheer();
        else
            student.ApplyTutorialPose(poseBool);
    }



    private void CancelMovement(PostStudent student)
    {
        if (!_movementByStudent.TryGetValue(student, out Coroutine movement)) return;
        StopCoroutine(movement);
        _movementByStudent.Remove(student);
    }



    private void StopAllMovement()
    {
        foreach (Coroutine movement in _movementByStudent.Values)
            if (movement != null) StopCoroutine(movement);
        _movementByStudent.Clear();
        _returnAfterStandUp.Clear();
    }



    private bool ValidateRequiredReferences()
    {
        bool valid = _stageController != null
            && _stageController.IsTutorialRuntime
            && _player != null
            && _stageSpots != null
            && _studentParent != null
            && _trainingEntryNavPoint != null
            && _waitingReturnNavPoint != null
            && _trainingBehaviorWeightSet != null;
        if (!valid)
            Debug.LogError("TutorialActorDirector 필수 참조 또는 Tutorial runtime config가 누락됐습니다.", this);
        if (valid && (_innocentTrainingSpot == null
            || !_innocentTrainingSpot.HasBehavior(_innocentTrainingBehavior)
            || _innocentTrainingBehavior.IsHazard()))
        {
            Debug.LogError("4-1 행동은 연결한 innocentTrainingSpot이 지원하는 비위험 행동이어야 합니다.", this);
            valid = false;
        }
        if (valid && _studentWorkBoostWaitingPoint == null)
        {
            Debug.LogError("6단계 studentWorkBoostWaitingPoint를 연결해야 합니다.", this);
            valid = false;
        }
        if (valid && (_studentWorkTrainingSpot == null
            || !_studentWorkTrainingSpot.HasBehavior(BehaviorType.Work)))
        {
            Debug.LogError("6단계 studentWorkTrainingSpot은 Work 행동을 지원해야 합니다.", this);
            valid = false;
        }
        return valid;
    }



    private bool ValidateMiniWaveComputerSeats(int count)
    {
        if (_miniWaveComputerSeats == null || _miniWaveComputerSeats.Length < count)
        {
            Debug.LogError(
                $"미니웨이브 학생 {count}명에게 필요한 컴퓨터 지정석이 부족합니다. "
                + $"TutorialActorDirector에 서로 다른 MonitorSpot을 {count}개 이상 연결해야 합니다.",
                this);
            return false;
        }

        HashSet<MonitorSpot> uniqueSeats = new();
        for (int i = 0; i < count; i++)
        {
            MonitorSpot seat = _miniWaveComputerSeats[i];
            if (seat == null)
            {
                Debug.LogError($"미니웨이브 컴퓨터 지정석 index {i} 참조가 없습니다.", this);
                return false;
            }
            if (!uniqueSeats.Add(seat))
            {
                Debug.LogError($"미니웨이브 컴퓨터 지정석 '{seat.name}'이 중복 연결됐습니다.", seat);
                return false;
            }
            if (!seat.HasBehavior(BehaviorType.Work))
            {
                Debug.LogError($"미니웨이브 컴퓨터 지정석 '{seat.name}'이 Work 행동을 지원하지 않습니다.", seat);
                return false;
            }
            if (!seat.IsUsable)
            {
                Debug.LogError($"미니웨이브 컴퓨터 지정석 '{seat.name}'이 준비 시점에 이미 사용 중입니다.", seat);
                return false;
            }
        }
        return true;
    }



    private bool ValidatePreparedMiniWaveComputerSeats()
    {
        HashSet<MonitorSpot> uniqueSeats = new();
        foreach (PostStudent student in _miniWaveStudents)
        {
            MonitorSpot seat = student != null ? student.SeatSpot : null;
            if (seat == null)
            {
                Debug.LogError($"미니웨이브 선발 학생 '{student?.name ?? "null"}'의 컴퓨터 지정석이 없습니다.", this);
                return false;
            }
            if (!uniqueSeats.Add(seat))
            {
                Debug.LogError($"미니웨이브 선발 학생들의 컴퓨터 지정석 '{seat.name}'이 중복됐습니다.", seat);
                return false;
            }
            if (!seat.IsUsable)
            {
                Debug.LogError($"미니웨이브 시작 시 컴퓨터 지정석 '{seat.name}'이 이미 사용 중입니다.", seat);
                return false;
            }
        }
        return true;
    }



    private bool ValidateRiskRoles()
    {
        if (_riskRoles == null || _riskRoles.Length != 8)
        {
            Debug.LogError("3단계 riskRoles는 정상 4개/위험 4개, 총 8개여야 합니다.", this);
            return false;
        }

        HashSet<BehaviorType> safe = new();
        HashSet<BehaviorType> hazard = new();
        foreach (TutorialRiskRoleBinding role in _riskRoles)
        {
            if (role.spot == null)
            {
                Debug.LogError("3단계 risk role의 고정 spot 참조가 없습니다.", this);
                return false;
            }
            if (!role.spot.HasBehavior(role.behavior))
            {
                Debug.LogError($"3단계 spot '{role.spot.name}'이 {role.behavior} 행동을 지원하지 않습니다.", role.spot);
                return false;
            }
            (role.isHazard ? hazard : safe).Add(role.behavior);
        }

        bool valid = safe.SetEquals(new[] { BehaviorType.Worship, BehaviorType.Game, BehaviorType.Sing, BehaviorType.Dance })
            && hazard.SetEquals(new[] { BehaviorType.Escape, BehaviorType.Hack, BehaviorType.Sing, BehaviorType.Smoke });
        if (!valid)
            Debug.LogError("3단계 행동 매핑은 P-09(숭배/탈출구 공격, 게임/해킹, 정상/저질 노래, 춤/흡연)와 일치해야 합니다.", this);
        return valid;
    }



    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int index = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[index]) = (list[index], list[i]);
        }
    }



    private void OnDestroy()
    {
        StopAllMovement();
        foreach (PostStudent student in _students)
        {
            if (student == null) continue;
            student.ClearTutorialFood();
            student.StopTutorialCheer();
            student.TutorialStandUpCompletedEvent -= OnTutorialStudentStandUpCompleted;
            if (_stageController != null)
                _stageController.UnregisterStudent(student);
        }
        _microwaveDisplayStudents.Clear();
        _bubbleAnchorByStudent.Clear();
    }
}



[Serializable]
public struct TutorialRiskRoleBinding
{
    public bool isHazard;
    public BehaviorType behavior;
    public BehaveSpot spot;
}
