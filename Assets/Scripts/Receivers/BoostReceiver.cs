using UnityEngine;
using UnityEngine.Events;

public class BoostReceiver : EffectReceiver
{
    public override Stat EffectedStat => null;
    public UnityEvent WorkTriggerEvent = new();
    public UnityEvent FrenzyTriggerEvent = new();

    private AttributeModifier boostTaskChanceMod;



    private void Awake()
    {
        boostTaskChanceMod = AttributeSystem.Instance.BoostTaskChanceMod;
    }

    protected override void ApplyEffect(EffectData data, HitInfo hitInfo)
    {
        BoostData boostData = data as BoostData;
        if (boostData == null) return;
        float workChance = boostTaskChanceMod.GetFinalValue(boostData.potency.workProbability);
        float frenzyChance = boostData.potency.frenzyProbability;

        float totalWeight = workChance + frenzyChance;
        float roll = UnityEngine.Random.value;

        // 3. °¡ÁßÄ¡ ÆÇÁ¤ (A ´çÃ· -> ¾Æ´Ï¸é B ´çÃ· -> ¾Æ´Ï¸é ²Î)
        if (roll < workChance)
        {
            WorkTriggerEvent?.Invoke();
        }
        else if (roll < workChance + frenzyChance)
        {
            FrenzyTriggerEvent?.Invoke();
        }
        //else
        //{
        //    Debug.Log($"[Boost] No Effect. (Roll: {roll})");
        //}
    }
}
