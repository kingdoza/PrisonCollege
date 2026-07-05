using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public abstract class CompositeNode : BT_Node
{
    protected List<BT_Node> children = new List<BT_Node>();

    public CompositeNode(List<BT_Node> children)
    {
        this.children = children;
    }

    // 부모 노드에 블랙보드가 주입될 때 자식들에게도 전파 (재귀)
    public override void SetBlackboard(Blackboard blackboard)
    {
        base.SetBlackboard(blackboard);
        foreach (var child in children)
        {
            child.SetBlackboard(blackboard);
        }
    }

    public override void Reset()
    {
        foreach (var child in children) child.Reset();
    }
}



public class Sequence : CompositeNode
{
    private int _currentIndex = 0;
    public Sequence(List<BT_Node> children) : base(children) { }

    public override NodeState Evaluate()
    {
        if (_currentIndex >= children.Count) return NodeState.Success;

        var result = children[_currentIndex].Evaluate();

        switch (result)
        {
            case NodeState.Success:
                _currentIndex++;
                if (_currentIndex >= children.Count)
                {
                    Reset(); // 전체 완료 시 리셋
                    return NodeState.Success;
                }
                return NodeState.Running; // 다음 자식을 위해 계속 진행

            case NodeState.Failure:
                Reset(); // 중간 실패 시 리셋
                return NodeState.Failure;

            case NodeState.Running:
                return NodeState.Running;
        }

        return NodeState.Failure;
    }

    public override void Reset()
    {
        base.Reset(); // 모든 자식 리셋
        _currentIndex = 0; // 내 인덱스 초기화
    }
}



public class Selector : CompositeNode
{
    private int _currentIndex = 0; // 현재 실행 중인 자식의 위치를 기억

    public Selector(List<BT_Node> children) : base(children) { }

    public override NodeState Evaluate()
    {
        // 기억하고 있는 인덱스부터 검사 시작
        while (_currentIndex < children.Count)
        {
            var result = children[_currentIndex].Evaluate();

            switch (result)
            {
                case NodeState.Success:
                    // 하나라도 성공하면 셀렉터 전체가 성공
                    Reset();
                    return NodeState.Success;

                case NodeState.Failure:
                    // 실패하면 다음 자식으로 넘어가서 검사 (루프 계속)
                    _currentIndex++;
                    continue;

                case NodeState.Running:
                    // 진행 중이면 현재 인덱스를 유지하고 리턴
                    // 다음 틱(Tick)에 이 인덱스부터 다시 Evaluate 실행
                    return NodeState.Running;
            }
        }

        // 모든 자식을 검사했는데 전부 Failure라면 전체 실패
        Reset();
        return NodeState.Failure;
    }

    public override void Reset()
    {
        base.Reset(); // 모든 자식 재귀적 리셋
        _currentIndex = 0; // 인덱스 초기화
    }
}



public class RandomSelector : CompositeNode
{
    private List<System.Func<float>> _weights;
    private BT_Node _selectedChild; // 현재 선택되어 실행 중인 자식

    public RandomSelector(List<BT_Node> children, List<System.Func<float>> weights) : base(children)
    {
        _weights = weights;
    }


    public RandomSelector(List<BT_Node> children) : base(children)
    {
        _weights = new List<System.Func<float>>();
        for (int i = 0; i < children.Count; i++)
        {
            _weights.Add(() => 1); // 모든 자식이 동일한 1의 가중치를 가짐
        }
    }

    public override NodeState Evaluate()
    {
        if (children.Count == 0) return NodeState.Failure;

        // 1. 선택된 자식이 없다면 새로 뽑기
        if (_selectedChild == null)
        {
            float totalWeight = 0;
            foreach (var w in _weights) totalWeight += Mathf.Max(0, w());
            if (totalWeight <= 0) return NodeState.Failure;

            float roll = UnityEngine.Random.Range(0, totalWeight);
            float cursor = 0;

            for (int i = 0; i < children.Count; i++)
            {
                cursor += _weights[i]();
                if (roll < cursor)
                {
                    _selectedChild = children[i];
                    break;
                }
            }
        }

        // 2. 선택된 자식 실행
        var result = _selectedChild.Evaluate();

        // 3. 실행이 끝났다면 참조 제거 (다음번에 새로 뽑도록)
        if (result != NodeState.Running)
        {
            Reset();
        }

        return result;
    }

    public override void Reset()
    {
        _selectedChild?.Reset();
        _selectedChild = null;
    }
}



public class PrioritySelector : CompositeNode
{
    private int _lastRunningIndex = -1;

    public PrioritySelector(List<BT_Node> children) : base(children) { }

    public override NodeState Evaluate()
    {
        for (int i = 0; i < children.Count; i++)
        {
            NodeState state = children[i].Evaluate();

            // 이번 프레임에 성공(Success)하거나 실행(Running) 중인 노드를 찾음
            if (state != NodeState.Failure)
            {
                // [중단 로직] 이전에 실행하던 노드가 있고, 그 노드보다 현재 노드의 우선순위가 높다면(인덱스가 작다면)
                if (_lastRunningIndex != -1 && _lastRunningIndex > i)
                {
                    children[_lastRunningIndex].Reset();
                }

                // 현재 실행 중인 인덱스 기록 (Running일 때만 유지, Success/Failure면 초기화)
                _lastRunningIndex = (state == NodeState.Running) ? i : -1;
                return state;
            }
        }

        // 모든 자식이 Failure를 반환한 경우
        _lastRunningIndex = -1;
        return NodeState.Failure;
    }

    public override void Reset()
    {
        base.Reset(); // 모든 자식 노드를 재귀적으로 Reset
        _lastRunningIndex = -1;
    }
}



public class ReactiveSelector : CompositeNode
{
    private int _runningChildIndex = -1;

    public ReactiveSelector(List<BT_Node> children) : base(children) { }

    public override NodeState Evaluate()
    {
        for (int i = 0; i < children.Count; i++)
        {
            NodeState state = children[i].Evaluate();

            // 1. 어떤 자식이 Running이나 Success를 반환했다면
            if (state != NodeState.Failure)
            {
                // 만약 이전에 실행 중이던 노드가 있고, 그게 지금 노드가 아니라면?
                // 즉, 우선순위가 높은 상위 노드가 조건을 만족해서 가로챘다면!
                if (_runningChildIndex != -1 && _runningChildIndex != i)
                {
                    children[_runningChildIndex].Reset(); // 기존 실행 중인 트리 강제 리셋(취소)
                }

                _runningChildIndex = (state == NodeState.Running) ? i : -1;
                return state;
            }
        }

        _runningChildIndex = -1;
        return NodeState.Failure;
    }

    public override void Reset()
    {
        base.Reset();
        _runningChildIndex = -1;
    }
}



public class InterruptSelector : CompositeNode
{
    private int _lastRunningIndex = -1;

    public InterruptSelector(List<BT_Node> children) : base(children) { }

    public override NodeState Evaluate()
    {
        for (int i = 0; i < children.Count; i++)
        {
            NodeState state = children[i].Evaluate();

            // 이번 프레임에 선택된 노드가 이전 프레임과 다르다면
            if (state != NodeState.Failure)
            {
                // 우선순위가 높은 노드가 가로챘거나, 상태가 바뀌었을 때
                if (_lastRunningIndex != -1 && _lastRunningIndex != i)
                {
                    // 이전에 실행 중이던 하위 트리를 뿌리까지 초기화
                    children[_lastRunningIndex].Reset();
                }

                _lastRunningIndex = (state == NodeState.Running) ? i : -1;
                return state;
            }
        }

        // 모든 자식이 Failure라면 기존 노드 리셋
        if (_lastRunningIndex != -1)
        {
            children[_lastRunningIndex].Reset();
            _lastRunningIndex = -1;
        }

        return NodeState.Failure;
    }

    public override void Reset()
    {
        if (_lastRunningIndex != -1) children[_lastRunningIndex].Reset();
        _lastRunningIndex = -1;
    }
}



public class ParallelNode : CompositeNode
{
    public ParallelNode(List<BT_Node> children) : base(children) { }

    public override NodeState Evaluate()
    {
        bool anyRunning = false;
        int successCount = 0;

        foreach (var child in children)
        {
            switch (child.Evaluate())
            {
                case NodeState.Success:
                    successCount++;
                    continue;
                case NodeState.Failure:
                    // 정책: 하나라도 실패하면 전체 실패로 간주
                    return NodeState.Failure;
                case NodeState.Running:
                    anyRunning = true;
                    continue;
                default:
                    continue;
            }
        }

        // 모든 자식이 Success라면 Success
        if (successCount == children.Count)
        {
            return NodeState.Success;
        }

        // 하나라도 Running 중이면 Running
        return anyRunning ? NodeState.Running : NodeState.Success;
    }

    public override void Reset()
    {
        base.Reset();
        foreach (var child in children)
        {
            child.Reset();
        }
    }
}



public class ParallelOR : CompositeNode
{
    public ParallelOR(List<BT_Node> children) : base(children) { }

    public override NodeState Evaluate()
    {
        bool anyRunning = false;
        bool anySuccess = false;
        int failureCount = 0;

        foreach (var child in children)
        {
            switch (child.Evaluate())
            {
                case NodeState.Success:
                    // 정책: 하나라도 성공하면 성공으로 간주 (즉시 반환하거나 플래그 설정)
                    anySuccess = true;
                    continue;
                case NodeState.Failure:
                    failureCount++;
                    continue;
                case NodeState.Running:
                    anyRunning = true;
                    continue;
            }
        }

        // 1. 모든 자식이 Failure라면 전체 Failure
        if (failureCount == children.Count)
        {
            return NodeState.Failure;
        }

        // 2. 하나라도 성공했다면 Success (Running보다 우선순위가 높을 때)
        if (anySuccess)
        {
            return NodeState.Success;
        }

        // 3. 실패하지 않았고 성공도 아직 없다면 (즉, 누군가는 Running 중)
        return anyRunning ? NodeState.Running : NodeState.Failure;
    }

    public override void Reset()
    {
        base.Reset();
        foreach (var child in children)
        {
            child.Reset();
        }
    }
}



public class LoopUntil : CompositeNode
{
    private System.Func<bool> _condition;

    // 생성자: 반복할 노드(child)와 탈출 조건(condition)을 받음
    public LoopUntil(System.Func<bool> condition, BT_Node child)
        : base(new List<BT_Node> { child })
    {
        _condition = condition;
    }

    public override NodeState Evaluate()
    {
        // 1. 조건을 만족하면 루프를 종료하고 Success 반환
        if (_condition != null && _condition())
        {
            Reset(); // 루프 탈출 시 자식들의 상태를 초기화
            return NodeState.Success;
        }

        // 2. 자식 노드가 있다면 실행
        if (children.Count > 0)
        {
            NodeState childState = children[0].Evaluate();

            // 만약 자식이 내부적으로 루프를 돌리는 Running 상태라면 계속 진행
            // 자식이 Success나 Failure를 뱉어도 조건이 안 맞으면 다시 돌림
        }

        // 3. 조건이 만족되지 않았으므로 트리는 이 노드에 머묾
        return NodeState.Running;
    }



    public override void Reset() { }
}