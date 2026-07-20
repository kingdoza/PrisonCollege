using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TutorialCourseDefinition", menuName = "Tutorial/Course Definition")]
public class TutorialCourseDefinition : ScriptableObject
{
    [SerializeField] private TutorialStepContent[] _steps = Array.Empty<TutorialStepContent>();

    [Header("Step 3 - 위험 행동 추가 정보")]
    [Tooltip("탈출구 공격, 해킹, 저질 노래, 흡연 설명을 각각 한 번씩 등록합니다.")]
    [SerializeField] private TutorialRiskBehaviorContent[] _riskBehaviorContents = Array.Empty<TutorialRiskBehaviorContent>();

    [Header("P-23 - Unity Editor에서 설정")]
    [Tooltip("0보다 커야 합니다. 정책에서 실제 값을 정하지 않았으므로 코드 기본값을 두지 않습니다.")]
    [SerializeField] private float _workConfirmationSeconds;

    [Header("P-26 확정 기본값")]
    [SerializeField] private float _miniWaveDuration = 30f;
    [SerializeField] private int _miniWaveEscapeFailureThreshold = 3;
    [SerializeField] private int _miniWaveStudentCount = 4;

    public float WorkConfirmationSeconds => _workConfirmationSeconds;
    public float MiniWaveDuration => _miniWaveDuration;
    public int MiniWaveEscapeFailureThreshold => _miniWaveEscapeFailureThreshold;
    public int MiniWaveStudentCount => _miniWaveStudentCount;

    public bool TryGetContent(TutorialStepId stepId, out TutorialStepContent content)
    {
        if (_steps != null)
        {
            for (int i = 0; i < _steps.Length; i++)
            {
                if (_steps[i].stepId == stepId)
                {
                    content = _steps[i];
                    return true;
                }
            }
        }

        content = default;
        return false;
    }

    public bool TryGetRiskBehaviorContent(
        TutorialRiskBehaviorInfoId behaviorId,
        out TutorialRiskBehaviorContent content)
    {
        if (_riskBehaviorContents != null)
        {
            for (int i = 0; i < _riskBehaviorContents.Length; i++)
            {
                if (_riskBehaviorContents[i].behaviorId == behaviorId)
                {
                    content = _riskBehaviorContents[i];
                    return true;
                }
            }
        }

        content = default;
        return false;
    }

    public IReadOnlyList<string> ValidateDefinition()
    {
        List<string> errors = new();
        HashSet<TutorialStepId> seen = new();
        if (_steps == null)
        {
            errors.Add("단계 문구 배열이 null입니다.");
            return errors;
        }

        foreach (TutorialStepContent step in _steps)
        {
            if (!seen.Add(step.stepId))
                errors.Add($"{step.stepId} 문구가 중복 등록됐습니다.");
        }

        TutorialRiskBehaviorInfoId[] requiredRiskContents =
        {
            TutorialRiskBehaviorInfoId.ExitAttack,
            TutorialRiskBehaviorInfoId.Hacking,
            TutorialRiskBehaviorInfoId.BadSinging,
            TutorialRiskBehaviorInfoId.Smoking,
        };
        HashSet<TutorialRiskBehaviorInfoId> seenRiskContents = new();
        if (_riskBehaviorContents == null)
        {
            errors.Add("3단계 위험 행동 추가 정보 배열이 null입니다.");
        }
        else
        {
            foreach (TutorialRiskBehaviorContent riskContent in _riskBehaviorContents)
            {
                if (!seenRiskContents.Add(riskContent.behaviorId))
                    errors.Add($"{riskContent.behaviorId} 위험 행동 추가 정보가 중복 등록됐습니다.");
                if (string.IsNullOrWhiteSpace(riskContent.title))
                    errors.Add($"{riskContent.behaviorId} 위험 행동 제목이 없습니다.");
                if (string.IsNullOrWhiteSpace(riskContent.description))
                    errors.Add($"{riskContent.behaviorId} 위험 행동 설명이 없습니다.");
            }
        }
        foreach (TutorialRiskBehaviorInfoId required in requiredRiskContents)
        {
            if (!seenRiskContents.Contains(required))
                errors.Add($"{required} 위험 행동 추가 정보가 없습니다.");
        }

        if (_workConfirmationSeconds <= 0f)
            errors.Add("P-23 workConfirmationSeconds를 Inspector에서 0보다 크게 설정해야 합니다.");
        if (_miniWaveDuration <= 0f)
            errors.Add("8단계 제한 시간은 0보다 커야 합니다.");
        if (_miniWaveEscapeFailureThreshold <= 0)
            errors.Add("8단계 탈출 실패 기준은 1 이상이어야 합니다.");
        if (_miniWaveStudentCount <= 0)
            errors.Add("8단계 학생 수는 1 이상이어야 합니다.");

        return errors;
    }
}

[Serializable]
public struct TutorialStepContent
{
    public TutorialStepId stepId;
    [Tooltip("표시 번호를 포함한 단계 제목")]
    public string title;
    public string subtitle;
    [TextArea(2, 8)] public string guide;
    [TextArea(1, 4)] public string objective;
    public string inputHint;
    [TextArea(1, 4)] public string completion;
}

[Serializable]
public struct TutorialRiskBehaviorContent
{
    public TutorialRiskBehaviorInfoId behaviorId;
    public string title;
    [TextArea(2, 6)] public string description;
}
