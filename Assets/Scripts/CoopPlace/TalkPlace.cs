using UnityEngine;

public class TalkPlace : CoopBehaviorPlace
{
    private Stat executeDuration;
    protected override BehaviorType RequiredBehavior => BehaviorType.Talk;


    protected override void Awake()
    {
        base.Awake();
        executeDuration = GetComponent<Stat>();
        executeDuration.Initialize(true);
        executeDuration.MaxReachEvent.AddListener(BreakUpCoop);
    }



    private void Update()
    {
        if (Phase != CoopPhase.Executing) return;
        executeDuration.Increase(Time.deltaTime);
    }



    public override void Execute()
    {
        base.Execute();
        executeDuration.Initialize(true);
    }


    protected override void ExecuteStudent(PostStudent student)
    {
        student.Blackboard.ExecuteTalk();
    }
}
