using UnityEngine;

[CreateAssetMenu(fileName = "NewOtakuPoster", menuName = "Item/OtakuPoster")]
public class OtakuPoster : PassiveItem
{
    public float chaosDecreasePercent;
    public bool isPoster;


    public override void Activate()
    {
        AttributeSystem.Instance.ChaosDecreaseMod.AddPercent(chaosDecreasePercent);
        AttributeSystem.Instance.IsOtakuPoster = isPoster;
    }
}
