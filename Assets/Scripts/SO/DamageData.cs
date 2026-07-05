using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDamageData", menuName = "Combat/Damage Data")]
public class DamageData : EffectData
{
    public override EffectReceiver GetActorReceiver(GameObject actor)
    {
        return actor.GetComponent<DamageReceiver>();
    }
}