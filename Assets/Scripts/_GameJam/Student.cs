using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;


public enum State
{
    Stop = 0,
    Crouch,
    Walk,
    Run,
    Naruto,
    Swim
}

public class Student : MonoBehaviour
{
    Action onArrived;
    public Animator animator;
        //루틴 간의 기다리는 시간
    public float waitingTime;

    private Vector3 targetPosition;
    private State currentState;
    private NavMeshAgent agent;
    private bool canDoOtherAction = true;


    [SerializeField] private Spot[] windowSpots;
    [SerializeField] private Spot doorSpot;
    [SerializeField] private Spot chairSpot;
    [SerializeField] private Spot[] standingSpots;
    [SerializeField] private Spot[] dancingSpots;
    [SerializeField] private Spot[] prayingSpots;
    [SerializeField] private Spot[] smokingSpots;


    [SerializeField] private Transform chairTransform;
    [SerializeField] private Transform doorTransform;
    [SerializeField] private Transform windowTransform;

    public Spot spot = null;



    private void SetCurrentState(State newState)
    {
        if (currentState == newState)
            return;

        if (Player.Instance.FloodSystem.GetComponent<FloodController>().isFull)
        {
            newState = State.Swim;
        }
        else if (Player.Instance.LightSwitch.isLight == false)
        {
            newState = State.Crouch;
        }
        currentState = newState;
        if (currentState == State.Run)
        {
            agent.speed = 4;
        }
        else if (currentState == State.Walk)
        {
            agent.speed = 2;
        }
        else if (currentState == State.Stop)
        {
            agent.speed = 0;
        }
        else if (currentState == State.Naruto)
        {
            agent.speed = 5;
        }
        else if (currentState == State.Crouch)
        {
            agent.speed = 1;
        }
        else if (currentState == State.Swim)
        {
            agent.speed = 2;
        }
        animator.SetFloat("MoveSpeed", (int)currentState);
    }

    private void Start()
    {
        Player.Instance.FloodSystem.GetComponent<FloodController>().FullEvent.AddListener(OnFloodFull);
        Player.Instance.LightSwitch.LightOffEvent.AddListener(OnLightSwitchOff);
        animator = GetComponent<Animator>();
        //agent = GetComponentInParent<NavMeshAgent>();
        agent = GetComponent<NavMeshAgent>();
        //StartBehaviourRoutine();
        SetCurrentState(State.Stop);
        targetPosition = GetNavMeshPosition(transform.position);
        //decideBehaviour();
        Sit();
    }


    private void Sit()
    {
        agent.enabled = false;
        transform.position = chairSpot.transform.position;
        agent.enabled = true;
        MoveAndAction(chairSpot, State.Walk);
        //StartBehaviourRoutine();
        StartCoroutine(RoutineDelay(12));
    }

    private void OnFloodFull(bool isFull)
    {
        if (isFull && currentState != State.Stop)
        {
            SetCurrentState(State.Swim);
        }
        else if (!isFull && currentState != State.Stop)
        {
            SetCurrentState(State.Walk);
        }
    }

    private void OnLightSwitchOff()
    {
        if (currentState == State.Stop)
            return;
        if (spot is DoorSpot || spot is WindowSpot)
        {
            SetCurrentState(State.Crouch);
        }
    }

    private bool HasReachedDestination()
    {
        if (agent.pathPending) return false;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                return true;
        }
        return false;
    }


    private bool IsTargetPositionNear()
    {
        Vector3 currentPosPlane = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 targetPosPlane = new Vector3(targetPosition.x, 0, targetPosition.z);
        float distance = Vector3.Distance(currentPosPlane, targetPosPlane);
        //Debug.Log(distance);
        return distance < 0.1f;
    }


    private void Update()
    {
        if (IsTargetPositionNear() == false && agent.enabled == true)
        {
            agent.SetDestination(targetPosition);
        }
        else
        {
            SetCurrentState(State.Stop);
            onArrived?.Invoke();
            onArrived = null;
        }

        // if (currentState == State.Stop)
        // {
        //     StartBehaviourRoutine();
        // }

        if (!agent.hasPath) return;

        Debug.DrawLine(
            agent.transform.position,
            agent.destination,
            Color.yellow
        );
    }

    //for debugg
    // void Start(){ startBehaviourRoutine();}

    public void StartBehaviourRoutine()
    {
        //waitingTime 만큼 기다렸다가 행동 시작
        float delay = UnityEngine.Random.Range(waitingTime - 1, waitingTime + 1) * GetMultiplier();
        if (spot && (spot is DoorSpot || spot is WindowSpot))
            delay = 6;
        else if (spot && (spot is SmokeSpot_d))
            delay = 10.5f;
        StartCoroutine(RoutineDelay(delay));
    }

    IEnumerator RoutineDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        decideBehaviour();
    }


    float GetMultiplier()
    {
        return Mathf.Pow(4f / 5f, ChaosSystem.chaos / 10f);
    }


    public State GetRandomMoveState()
    {
        float probA = 0.7f * Mathf.Pow(4f / 5f, ChaosSystem.chaos / 10f);
        float delta = 0.7f - probA;
        float probB = 0.2f + delta * (2f / 3f);
        float probC = 0.1f + delta * (1f / 3f);

        float r = UnityEngine.Random.value;
        if (r < probA)
        return State.Walk;
        else if (r < probA + probB)
            return State.Run;
        else
            return State.Naruto;
    }



    private void decideBehaviour()
    {
        //UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
        float randValue = UnityEngine.Random.Range(0f, 1f) * 100;
        //int randValue = UnityEngine.Random.Range(1, 100 + 1);

        //담배 디버깅
        // MoveAndAction(smokingSpots.GetRandom(), State.Walk); 
        // return;
        //


        // if (randValue <= 90)
        // {
        //     MoveAndAction(doorSpot);
        // }
        // else
        // {
        //     MoveAndAction(windowSpots.GetRandom());
        // }
        // return;

        if (randValue <= 20)
        {
            MoveAndAction(chairSpot);
        }
        else if (randValue <= 40)
        {
            MoveAndAction(windowSpots.GetRandom());
        }
        else if (randValue <= 60)
        {
            MoveAndAction(doorSpot);
        }
        else if (randValue <= 70)
        {
            MoveAndAction(standingSpots.GetRandom());
        }
        else if (randValue <= 80)
        {
            MoveAndAction(dancingSpots.GetRandom());
        }
        else if (randValue <= 90)
        {
            MoveAndAction(prayingSpots.GetRandom());
        }
        else
        {
            MoveAndAction(smokingSpots.GetRandom());
        }

        //MoveAndAction(chairSpot, State.Walk, "Type");

        // if(randValue <= 50)
        // {
        //     MoveAndAction(windowSpots.GetRandom(), State.Naruto, "TryOpenDoor");
        // }
        // else
        // {
        //     MoveAndAction(doorSpot, State.Run, "Type");
        // }

        // if (randValue <= 33)
        // {
        //     MoveAndAction(windowSpots.GetRandom(), State.Naruto);
        // }
        // else if (randValue <= 66)
        // {
        //     MoveAndAction(chairSpot, State.Walk);
        // }
        // else
        // {
        //     MoveAndAction(doorSpot, State.Run);
        // }
    }



    private void RunTo(Vector3 pos)
    {
        SetCurrentState(State.Run);
        targetPosition = GetNavMeshPosition(pos);
    }


    public void WalkTo(Vector3 pos)
    {
        SetCurrentState(State.Walk);
        targetPosition = GetNavMeshPosition(pos);
    }



    private void NarutoTo(Vector3 pos)
    {
        SetCurrentState(State.Naruto);
        targetPosition = GetNavMeshPosition(pos);
    }



    public void Stop()
    {
        SetCurrentState(State.Stop);
        targetPosition = GetNavMeshPosition(transform.position);
    }



    private void CrouchTo(Vector3 pos)
    {
        SetCurrentState(State.Crouch);
        targetPosition = GetNavMeshPosition(pos);
    }



    private void SwimTo(Vector3 pos)
    {
        SetCurrentState(State.Swim);
        targetPosition = GetNavMeshPosition(pos);
    }



    private void Type()
    {
        WalkTo(chairTransform.position);
        //onArrived = StartBehaviourRoutine;
        onArrived = () => {
            transform.rotation = Quaternion.LookRotation(chairTransform.forward);
            animator.SetTrigger("Type");
            StartBehaviourRoutine();
        };
    }



    private void TryOpenDoor()
    {
        WalkTo(doorTransform.position);
        //onArrived = StartBehaviourRoutine;
        //onArrived = () => animator.CrossFade("OpenDoorOutwards", 0.3f);
        onArrived = () => {
            transform.rotation = Quaternion.LookRotation(doorTransform.forward);
            animator.SetTrigger("TryOpenDoor");
            StartBehaviourRoutine();
        };
    }



    private void OpenDoor()
    {
        WalkTo(doorTransform.position);
        //onArrived = StartBehaviourRoutine;
        //onArrived = () => animator.CrossFade("OpenDoorOutwards", 0.3f);
        onArrived = () => {
            transform.rotation = Quaternion.LookRotation(doorTransform.forward);
            animator.applyRootMotion = true;
            animator.SetTrigger("OpenDoor");
            StartCoroutine(DeapplyRootMotion(5));
            StartBehaviourRoutine();
        };
    }



    private void Dance()
    {
        int value = UnityEngine.Random.value < 0.5f ? 1 : 2;
        animator.SetTrigger("Dance" + value);
    }


    private int health = 100;

    public AudioSource[] audioHit;
    public AudioSource[] audioHurtVoice;
    public void TakeDamage(int damage)
    {
        audioHit[UnityEngine.Random.Range(0,audioHit.Length)].Play();
        audioHurtVoice[UnityEngine.Random.Range(0,audioHurtVoice.Length)].Play();
        health -= damage;
        Debug.Log(health);
        if (health <= 0)
        {
            Die();
        }
    }


    private void Die()
    {
        // Stop();
        // SetCurrentState(State.Walk);
        Stop();
        onArrived = null;
        StopAllCoroutines();
        CancelInvoke();
        animator.applyRootMotion = true;
        animator.SetTrigger("Die1");
        GetComponent<Collider>().enabled = false;
        if (spot)
        {
            if (!(spot is SmokeSpot_d || spot is DoorSpot || spot is WindowSpot))
            {
                ChaosSystem.chaos += 10;
                Warning warning = Instantiate(
                    Player.Instance.warningPrefab,
                    Player.Instance.canvas.transform,
                    false   // ★ 중요
                ).GetComponent<Warning>();
                Player.Instance.warningSource.PlayOneShot(Player.Instance.warningSource.clip);
                warning.Play(string.Format("무고한 대학원생 폭행!! : 혼란 + {0}", 10));
            }
        }
        if (spot && spot.student)
        {
            spot.student = null;
        }
        spot = null;
        Invoke("Respwan", 3);
    }


    private void Respwan()
    {
        //SetCurrentState(State.Walk);
        health = 100;
        animator.applyRootMotion = false;
        agent.enabled = true;
        GetComponent<CapsuleCollider>().isTrigger = false;
        GetComponent<CapsuleCollider>().enabled = true;
        //Sit();
        GameObject clone = Instantiate(gameObject);
        Destroy(gameObject);
    }



    IEnumerator DeapplyRootMotion(float delay)
    {
        yield return new WaitForSeconds(delay);
        animator.applyRootMotion = false;
        agent.enabled = true;
        GetComponent<CapsuleCollider>().isTrigger = false;
    }



    private void JumpWindow()
    {
        NarutoTo(windowTransform.position);
        onArrived = () => {
            transform.rotation = Quaternion.LookRotation(windowTransform.forward);
            animator.applyRootMotion = true;
            animator.SetTrigger("JumpWindow");
            StartCoroutine(DeapplyRootMotion(5));
            StartBehaviourRoutine();
        };
    }


    private Vector3 GetNavMeshPosition(Vector3 pos)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(pos, out hit, 1f, NavMesh.AllAreas))
        {
            //Debug.Log(hit.position);
            return hit.position; // NavMesh 위의 정확한 높이
        }
        return targetPosition; // 실패 시 원래 위치
    }



    private void ExecuteMove(State newState, Vector3 pos)
    {
        if (newState == State.Stop)
        {
            Stop();
        }
        else if (newState == State.Crouch)
        {
            CrouchTo(pos);
        }
        else if (newState == State.Walk)
        {
            WalkTo(pos);
        }
        else if (newState == State.Run)
        {
            RunTo(pos);
        }
        else if (newState == State.Naruto)
        {
            NarutoTo(pos);
        }
        else if (newState == State.Swim)
        {
            SwimTo(pos);
        }
    }



    public void MoveAndAction(Spot newSpot, State newState, string animName, bool applyRoot = false)
    {
        if (newSpot.student)
        {
            StartCoroutine(RoutineDelay(1));
            return;
        }

        agent.updateRotation = true;
        if (spot)
        {
            spot.student = null;
        }
        spot = newSpot;
        spot.student = this;
        ExecuteMove(newState, newSpot.transform.position);
        onArrived = () => {
            agent.updateRotation = false;
            transform.rotation = Quaternion.LookRotation(newSpot.transform.forward);
            if (applyRoot)
            {
                animator.applyRootMotion = true;
                agent.enabled = false;
                GetComponent<CapsuleCollider>().isTrigger = true;
                StartCoroutine(DeapplyRootMotion(5));
            }
            animator.SetTrigger(animName);
            //animator.SetTrigger(animName);
            StartBehaviourRoutine();
        };
    }


    public void EscapeDoor(Spot newSpot)
    {
        animator.SetFloat("MoveSpeed", (int)State.Run);
        (newSpot as DoorSpot).door.OpenDoor();
        //PlayDoorOpenSound();
        animator.applyRootMotion = true;
        agent.enabled = false;
        GetComponent<CapsuleCollider>().isTrigger = true;
        Invoke("Escape", 2.0f);
    }


    public void EscapeWindow(Spot newSpot)
    {
        animator.SetTrigger("JumpWindow");
        animator.applyRootMotion = true;
        agent.enabled = false;
        GetComponent<CapsuleCollider>().isTrigger = true;
        Invoke("Escape", 2f);
    }


    public void MoveAndAction(Spot newSpot)
    {
        MoveAndAction(newSpot, GetRandomMoveState());
    }


    public void MoveAndAction(Spot newSpot, State newState)
    {
        if (newSpot.student)
        {
            StartCoroutine(RoutineDelay(1));
            return;
        }

        agent.updateRotation = true;
        if (spot)
        {
            spot.student = null;
        }
        spot = newSpot;
        spot.student = this;
        ExecuteMove(newState, newSpot.transform.position);
        onArrived = () => {
            spot.isArrived = true;
            if (newSpot.GetAnimName().Equals("Type") && Extension.Check(0.05f))
            {
                Invoke("SyetemHack", 2f);
            }
            agent.updateRotation = false;
            if (newSpot.HasToTurn())
                transform.rotation = Quaternion.LookRotation(newSpot.transform.forward);
            Debug.Log(spot.GetIsRootTrans());
            if (spot.GetIsRootTrans())
            {
                animator.applyRootMotion = true;
                agent.enabled = false;
                GetComponent<CapsuleCollider>().isTrigger = true;
                StartCoroutine(DeapplyRootMotion(5));
            }
            if (spot.GetIsEscape())
            {
                Invoke("Escape", 2.0f);
            }
            animator.SetTrigger(spot.GetAnimName());
            //animator.SetTrigger(animName);
            StartBehaviourRoutine();
            if (spot is DoorSpot)
            {
                DoorSpot doorSpot = (DoorSpot)spot;
                if (doorSpot.door.isUpgraded == false)
                    (spot as DoorSpot).door.OpenDoor();
            }
        };
    }

    public GameObject hackingVFX;

    private void SyetemHack()
    {
        GameObject temp = Instantiate(hackingVFX);
        temp.transform.position = new Vector3(hackingVFX.transform.position.x, hackingVFX.transform.position.y+2f, hackingVFX.transform.position.z);
        temp.GetComponent<ParticleSystem>().Play();

        IEnumerator EndHackVFXRoutine()
        {
            yield return new WaitForSeconds(1f);
            temp.GetComponent<ParticleSystem>().Stop();
            Destroy(temp);
        }
        StartCoroutine(EndHackVFXRoutine());
        Player.Instance.LightSwitch.OffLight();
    }


    public GameSystem gameSystem;
    private void Escape()
    {
        if (spot && spot.student)
        {
            spot.student = null;
        }
        spot = null;
        ChaosSystem.chaos += 30;
        Warning warning = Instantiate(
            Player.Instance.warningPrefab,
            Player.Instance.canvas.transform,
            false   // ★ 중요
        ).GetComponent<Warning>();
        Player.Instance.warningSource.PlayOneShot(Player.Instance.warningSource.clip);
        warning.Play(string.Format("대학원생 탈출!! : 혼란 + {0}", 30));
        gameSystem.UpdateEscapeCounter();
        Destroy(gameObject);
    }

    private void SpotAction()
    {
        
    }

    void PlayWoodPunchSound()
    {
        // 지정된 위치에 일회성 사운드 생성 (자동 파괴됨)
        AudioSource.PlayClipAtPoint(Player.Instance.audioPlayer.woodPunchClip, transform.position + Vector3.up * 1.5f);
    }


    void PlayGlassBreakSound()
    {
        AudioSource.PlayClipAtPoint(Player.Instance.audioPlayer.glassBreakSound, transform.position + Vector3.up * 1.5f);
    }


    void PlayWoodBreakSound()
    {
        AudioSource.PlayClipAtPoint(Player.Instance.audioPlayer.woodBreakSound, transform.position + Vector3.up * 1.5f);
    }

    void PlayDoorOpenSound()
    {
        AudioSource.PlayClipAtPoint(Player.Instance.audioPlayer.doorOpenSound, transform.position + Vector3.up * 1.5f);
    }

    // IEnumerator PlayAnimNextFrame(string name, bool applyRoot) {
    //     yield return null; // 한 프레임 대기
    //     if (applyRoot) 
    //     {
    //         animator.applyRootMotion = true;
    //         StartCoroutine(DeapplyRootMotion(5));
    //     }
    //     animator.SetTrigger(name);
    // }
}



public static class ListExtensions
{
    /// <summary>
    /// Transform 배열에서 랜덤 원소 하나 반환
    /// </summary>
    public static Spot GetRandom(this Spot[] array)
    {
        if (array == null || array.Length == 0)
            return null;

        int index = UnityEngine.Random.Range(0, array.Length);
        return array[index];
    }

    /// <summary>
    /// List<Transform>에서도 사용 가능
    /// </summary>
    public static Spot GetRandom(this IList<Spot> list)
    {
        if (list == null || list.Count == 0)
            return null;

        int index = UnityEngine.Random.Range(0, list.Count);
        return list[index];
    }
}