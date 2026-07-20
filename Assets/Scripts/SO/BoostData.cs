using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBoostData", menuName = "Combat/Boost Data")]
public class BoostData : EffectData
{
    public BoostPotency potency;
    [Tooltip("튜토리얼 연수용 asset에서만 켭니다. 정규 부스터는 false를 유지합니다.")]
    [SerializeField] private bool _ignorePassiveProbabilityModifiers;

    public bool IgnorePassiveProbabilityModifiers => _ignorePassiveProbabilityModifiers;
    public override EffectReceiver GetActorReceiver(GameObject actor)
    {
        return actor.GetComponent<BoostReceiver>();
    }
}



[Serializable]
public class BoostPotency
{
    public float workProbability;
    public float frenzyProbability;
}
