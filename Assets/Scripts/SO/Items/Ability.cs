using UnityEngine;

[CreateAssetMenu(fileName = "NewAbility", menuName = "Item/Ability")]
public class Ability : ScriptableObject
{
    public virtual void OnEquip(GameObject owner) { }
    public virtual void OnAttack(GameObject owner, GameObject target) { }
    public virtual void OnUpdate(GameObject owner) { }
    public virtual void OnActivate(GameObject target) { }
}
