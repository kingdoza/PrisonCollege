using UnityEngine;
using UnityEngine.Events;

public class LabLightSystem : SceneSingleton<LabLightSystem>
{
    [SerializeField] private GameObject _toggleableLightGroup;
    [SerializeField] private GameObject _reflectionGroup;
    public bool IsLightsOn => _toggleableLightGroup.activeSelf;

    [HideInInspector] public UnityEvent LightsOffEvent = new();
    [HideInInspector] public UnityEvent LightsOnEvent = new();
    private Color _originalAmbientColor;



    protected override void Awake()
    {
        _originalAmbientColor = RenderSettings.ambientLight;
        base.Awake();
    }



    private void Start()
    {
        TurnOn();
    }



    public void TurnOff()
    {
        if (!IsLightsOn) return;
        Debug.Log("LightOff");
        _toggleableLightGroup.SetActive(false);
        //RenderSettings.ambientLight = Color.black;
        RenderSettings.ambientLight = new Color(0/255f, 0/255f, 0/255f);
        _reflectionGroup.SetActive(false);
        LightsOffEvent?.Invoke();
    }



    public void TurnOn()
    {
        if (IsLightsOn) return;
        Debug.Log("LightOn");
        _toggleableLightGroup.SetActive(true);
        RenderSettings.ambientLight = _originalAmbientColor;
        _reflectionGroup.SetActive(true);
        LightsOnEvent?.Invoke();
    }



    public void HackDefensed()
    {

    }
}
