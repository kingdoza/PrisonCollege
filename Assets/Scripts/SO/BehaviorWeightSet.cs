using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;


[CreateAssetMenu(fileName = "NewBehaviorWeightSet", menuName = "Combat/Behavior Weight Set")]
public class BehaviorWeightSet : WeightedSetSO<BehaviorType, BehaviorChance>
{
    public void ModifyChance(BehaviorType behaviorType, float multiply)
    {
        foreach (BehaviorChance behaviorChance in WeightedElements)
        {
            if (behaviorChance.Value != behaviorType) continue;
            behaviorChance.MultiplyChance(multiply);
        }
    }


    protected override BehaviorChance CloneEntry(BehaviorChance entry)
    {
        // 필드값을 그대로 복사한 새 객체 생성 (생성자 활용)
        return new BehaviorChance(entry.Value, entry.Chance);
    }


    public BehaviorWeightSet CreateDeepCopy()
    {
        // 1. ScriptableObject 껍데기 복사
        BehaviorWeightSet clone = Instantiate(this);

        // 2. 내부 리스트 및 원소들 깊은 복사 진행
        clone.InitializeCopy(this.WeightedElements);

        return clone;
    }
}



[System.Serializable]
public class BehaviorChance : IWeightedEntry<BehaviorType>
{
    [SerializeField] private BehaviorType _behaviorType;
    public BehaviorType Value => _behaviorType;
    [SerializeField] private float _chance;
    public float Chance => _chance;

    public BehaviorChance(BehaviorType type, float chance)
    {
        _behaviorType = type;
        _chance = chance;
    }

    public void MultiplyChance(float multiplier) => _chance *= multiplier;
}



public enum BehaviorSafety { Safe, Hazard }

[AttributeUsage(AttributeTargets.Field)]
public class BehaviorInfoAttribute : Attribute
{
    public BehaviorSafety Safety { get; }
    public BehaviorInfoAttribute(BehaviorSafety safety) => Safety = safety;
}



[System.Flags]
public enum BehaviorType
{
    [BehaviorInfo(BehaviorSafety.Safe)] None = 0,
    [BehaviorInfo(BehaviorSafety.Safe)] Work = 1 << 0,
    [BehaviorInfo(BehaviorSafety.Safe)] LookAround = 1 << 1,
    [BehaviorInfo(BehaviorSafety.Safe)] UseMicrowave = 1 << 2,
    [BehaviorInfo(BehaviorSafety.Hazard)] Escape = 1 << 3,
    [BehaviorInfo(BehaviorSafety.Hazard)] RushThrough = 1 << 4,
    [BehaviorInfo(BehaviorSafety.Hazard)] Fight = 1 << 5,
    [BehaviorInfo(BehaviorSafety.Hazard)] Smoke = 1 << 6,
    [BehaviorInfo(BehaviorSafety.Hazard)] Tackle = 1 << 7,
    [BehaviorInfo(BehaviorSafety.Hazard)] Hack = 1 << 8,
    [BehaviorInfo(BehaviorSafety.Safe)] Game = 1 << 9,

    //추가 예정
    [BehaviorInfo(BehaviorSafety.Safe)] Talk = 1 << 10,
    [BehaviorInfo(BehaviorSafety.Safe)] Dance = 1 << 11,
    [BehaviorInfo(BehaviorSafety.Safe)] Worship = 1 << 12,
    [BehaviorInfo(BehaviorSafety.Safe)] Sports = 1 << 13,
    [BehaviorInfo(BehaviorSafety.Safe)] Sleep = 1 << 14,
    [BehaviorInfo(BehaviorSafety.Safe)] SitFloor = 1 << 15,
    [BehaviorInfo(BehaviorSafety.Safe)] SitChair = 1 << 16,

    [BehaviorInfo(BehaviorSafety.Safe)] Sing = 1 << 17,
}