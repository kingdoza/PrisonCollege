using Unity.VisualScripting;
using UnityEngine;

public enum EffectType { Damage, Boost }

[CreateAssetMenu(fileName = "NewEffectData", menuName = "Combat/Effect Data")]
public class EffectData : ScriptableObject
{
    public float value;          // 수치
    public GameObject effectVisualPrefab; // 피격 시 생성될 이펙트 (선택 사항)

    public virtual EffectReceiver GetActorReceiver(GameObject actor)
    {
        return actor.GetComponent<EffectReceiver>();
    }
}