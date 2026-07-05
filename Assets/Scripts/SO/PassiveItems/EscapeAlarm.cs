using UnityEngine;

[CreateAssetMenu(fileName = "NewEscapeAlarm", menuName = "Item/EscapeAlarm")]
public class EscapeAlarm : PassiveItem
{
    public bool isExitAlarm;




    public override void Activate()
    {
        AttributeSystem.Instance.IsExitAlarm = isExitAlarm;
    }
}
