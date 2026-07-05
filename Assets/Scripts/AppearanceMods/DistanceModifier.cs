using UnityEngine;

public abstract class DistanceModifier : AppearanceModifier
{
    [SerializeField] private Transform _targetPart;
    [SerializeField] private Vector3 _offset;

    protected override void ModifyAppearance()
    {
        _targetPart.GetComponent<Rigidbody>().isKinematic = true;
        _targetPart.transform.position += _attributeModifier.GetFinalValue(1) * _offset;
    }
}
