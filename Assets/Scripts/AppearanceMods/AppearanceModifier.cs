using UnityEngine;

public abstract class AppearanceModifier : MonoBehaviour
{
    protected AttributeModifier _attributeModifier;

    private void Start()
    {
        _attributeModifier = GetItemAttribute();
        if (_attributeModifier == null) return;
        //Debug.Log($"{name} modifier {attributeModifier.GetFinalValue()}");
        ModifyAppearance();
    }



    protected abstract AttributeModifier GetItemAttribute();
    protected abstract void ModifyAppearance();
}
