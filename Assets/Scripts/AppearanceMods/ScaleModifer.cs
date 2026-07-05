using UnityEngine;

public abstract class ScaleModifer : AppearanceModifier
{
    [SerializeField] protected Transform _targetPart;
    [SerializeField] protected bool _lockX;
    [SerializeField] protected bool _lockY;
    [SerializeField] protected bool _lockZ;

    protected override void ModifyAppearance()
    {
        float multiplier = _attributeModifier.GetFinalValue(1);
        Vector3 currentScale = _targetPart.localScale;

        // 각 축에 대해 lock이 걸려있으면 원래 값(currentScale), 아니면 곱한 값 적용
        float nextX = _lockX ? currentScale.x : currentScale.x * multiplier;
        float nextY = _lockY ? currentScale.y : currentScale.y * multiplier;
        float nextZ = _lockZ ? currentScale.z : currentScale.z * multiplier;

        _targetPart.localScale = new Vector3(nextX, nextY, nextZ);
    }
}
