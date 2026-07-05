using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBoostData", menuName = "Combat/Boost Data")]
public class BoostData : EffectData
{
    public BoostPotency potency;
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
