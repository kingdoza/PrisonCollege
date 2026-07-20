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
    private float _originalAmbientIntensity;
    private float _originalReflectionIntensity;
    private ReflectionProbe[] _reflectionProbes = new ReflectionProbe[0];
    private float[] _originalProbeIntensities = new float[0];



    protected override void Awake()
    {
        CaptureLightingState();
        base.Awake();
    }



    private void Start()
    {
        TurnOn();
    }



    public void TurnOff()
    {
        if (!IsLightsOn) return;
        CaptureLightingState();
        Debug.Log("LightOff");
        _toggleableLightGroup.SetActive(false);
        RenderSettings.ambientLight = Color.black;
        RenderSettings.ambientIntensity = 0f;
        RenderSettings.reflectionIntensity = 0f;
        SetReflectionProbeIntensities(0f);
        DynamicGI.UpdateEnvironment();
        LightsOffEvent?.Invoke();
    }



    public void TurnOn()
    {
        if (IsLightsOn) return;
        Debug.Log("LightOn");
        _toggleableLightGroup.SetActive(true);
        RenderSettings.ambientLight = _originalAmbientColor;
        RenderSettings.ambientIntensity = _originalAmbientIntensity;
        RenderSettings.reflectionIntensity = _originalReflectionIntensity;
        if (_reflectionGroup != null && !_reflectionGroup.activeSelf)
            _reflectionGroup.SetActive(true);
        RestoreReflectionProbeIntensities();
        DynamicGI.UpdateEnvironment();
        RenderRealtimeReflectionProbes();
        LightsOnEvent?.Invoke();
    }



    private void CaptureLightingState()
    {
        _originalAmbientColor = RenderSettings.ambientLight;
        _originalAmbientIntensity = RenderSettings.ambientIntensity;
        _originalReflectionIntensity = RenderSettings.reflectionIntensity;

        _reflectionProbes = _reflectionGroup != null
            ? _reflectionGroup.GetComponentsInChildren<ReflectionProbe>(true)
            : new ReflectionProbe[0];
        _originalProbeIntensities = new float[_reflectionProbes.Length];
        for (int i = 0; i < _reflectionProbes.Length; i++)
        {
            ReflectionProbe probe = _reflectionProbes[i];
            _originalProbeIntensities[i] = probe != null ? probe.intensity : 0f;
        }
    }



    private void SetReflectionProbeIntensities(float intensity)
    {
        foreach (ReflectionProbe probe in _reflectionProbes)
        {
            if (probe != null)
                probe.intensity = intensity;
        }
    }



    private void RestoreReflectionProbeIntensities()
    {
        int count = Mathf.Min(_reflectionProbes.Length, _originalProbeIntensities.Length);
        for (int i = 0; i < count; i++)
        {
            ReflectionProbe probe = _reflectionProbes[i];
            if (probe != null)
                probe.intensity = _originalProbeIntensities[i];
        }
    }



    private void RenderRealtimeReflectionProbes()
    {
        foreach (ReflectionProbe probe in _reflectionProbes)
        {
            if (probe != null
                && probe.isActiveAndEnabled
                && probe.mode == UnityEngine.Rendering.ReflectionProbeMode.Realtime)
                probe.RenderProbe();
        }
    }



    public void HackDefensed()
    {

    }
}
