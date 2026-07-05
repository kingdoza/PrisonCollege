using UnityEngine;

public class AlarmDecorator : ItemDecorator
{
    [SerializeField] private Transform _exitSpotParent;
    
    protected override void Start()
    {
        base.Start();
        EscapeDetector[] detectors = GetComponentsInChildren<EscapeDetector>();
        ExitSpot[] exitSpots = _exitSpotParent.GetComponentsInChildren<ExitSpot>();
        for (int i = 0; i < detectors.Length; i++)
        {
            detectors[i].exitSpot = exitSpots[i];
        }
    }


    protected override bool GetItemActivation()
    {
        return AttributeSystem.Instance.IsExitAlarm;
    }
}
