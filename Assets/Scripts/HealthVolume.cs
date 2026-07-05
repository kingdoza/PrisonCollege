using UnityEngine;
using UnityEngine.Rendering; // 추가
using UnityEngine.Rendering.Universal; // URP 전용 네임스페이스

public class HealthVolume : MonoBehaviour
{
    [SerializeField] private Volume _volume;
    [Header("ChromaticAberration")]
    [SerializeField] private Vector2 _chromIntensityRange;
    [Header("ColorAdjustments")]
    [SerializeField] private Vector2 _saturationRange;
    [Header("Vignette")]
    [SerializeField] private Gradient _colorRange;
    [SerializeField] private Vector2 _vigIntensityRange;
    [Header("DepthOfField")]
    [SerializeField] private Vector2 _maxRadiusRange;
    [SerializeField] private float _dofActiveThreshold;

    private ChromaticAberration _chromaticAberration;
    private ColorAdjustments _colorAdjustments;
    private Vignette _vignette;
    private DepthOfField _depthOfField;



    private void Awake()
    {
        _volume.profile.TryGet(out _chromaticAberration);
        _volume.profile.TryGet(out _colorAdjustments);
        _volume.profile.TryGet(out _vignette);
        _volume.profile.TryGet(out _depthOfField);
    }



    public void AdjustVolume(float healthRatio)
    {
        float damageRatio = Mathf.Clamp01(1 - healthRatio);
        _chromaticAberration.intensity.value = Mathf.Lerp(_chromIntensityRange.x, _chromIntensityRange.y, damageRatio);
        _colorAdjustments.saturation.value = Mathf.Lerp(_saturationRange.x, _saturationRange.y, damageRatio);
        _vignette.color.value = _colorRange.Evaluate(damageRatio);
        _vignette.intensity.value = Mathf.Lerp(_vigIntensityRange.x, _vigIntensityRange.y, damageRatio);
        if (healthRatio > _dofActiveThreshold)
        {
            _depthOfField.active = false;
        }
        else
        {
            float normalizedVal = 1f - Mathf.Lerp(0, 1, healthRatio);
            _depthOfField.active = true;
            _depthOfField.gaussianMaxRadius.value = Mathf.Lerp(_maxRadiusRange.x, _maxRadiusRange.y, normalizedVal);
        }
    }
}
