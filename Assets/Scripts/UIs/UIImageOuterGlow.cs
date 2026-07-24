using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class UIImageOuterGlow : BaseMeshEffect, IMaterialModifier
{
    private const string ShaderResourcePath = "Shaders/UIOuterGlow";
    private const string ShaderFallbackName = "UI/PrisonCollege/Outer Glow";
    private const int MaxUiVertexCount = 65000;

    private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
    private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");

    [Header("Outer Glow")]
    [SerializeField, ColorUsage(false, true)]
    [Tooltip("이미지 바깥쪽으로 번지는 발광 색상입니다. Alpha는 기본 광륜 투명도입니다.")]
    private Color _glowColor = new(0.31f, 0.87f, 0.93f, 0.65f);

    [SerializeField, Range(0f, 32f)]
    [Tooltip("원본 Image 바깥으로 번지는 거리입니다. Canvas의 UI 단위를 사용합니다.")]
    private float _glowWidth = 8f;

    [SerializeField, Range(0f, 8f)]
    [Tooltip("광륜의 최종 밝기입니다. 0이면 Glow를 그리지 않습니다.")]
    private float _glowIntensity = 1f;

    [SerializeField, Range(4, 16)]
    [Tooltip("한 원을 구성하는 방향 샘플 수입니다. 높을수록 둥글지만 UI 정점 수가 증가합니다.")]
    private int _directionSamples = 8;

    [SerializeField, Range(1, 4)]
    [Tooltip("안쪽에서 바깥쪽까지 겹쳐 그릴 원의 수입니다. 높을수록 부드럽지만 UI 정점 수가 증가합니다.")]
    private int _softnessRings = 3;

    [Header("Pulse (Optional)")]
    [SerializeField]
    [Tooltip("Time.timeScale과 무관하게 Glow 밝기를 반복 변화시킵니다.")]
    private bool _pulse;

    [SerializeField, Min(0f)]
    [Tooltip("기본 Glow Intensity를 기준으로 위아래로 변화할 절대 크기입니다.")]
    private float _pulseAmount = 0.25f;

    [SerializeField, Min(0f)]
    [Tooltip("초당 반복 횟수입니다.")]
    private float _pulseFrequency = 1f;

    private readonly List<UIVertex> _sourceVertices = new();
    private readonly List<UIVertex> _outputVertices = new();

    private Image _image;
    private Material _runtimeMaterial;
    private bool _missingShaderLogged;
    private bool _vertexLimitWarningLogged;

    public Color GlowColor
    {
        get => _glowColor;
        set
        {
            _glowColor = value;
            ApplyMaterialProperties(CurrentIntensity);
        }
    }

    public float GlowWidth
    {
        get => _glowWidth;
        set
        {
            _glowWidth = Mathf.Max(0f, value);
            SetVerticesDirty();
        }
    }

    public float GlowIntensity
    {
        get => _glowIntensity;
        set
        {
            _glowIntensity = Mathf.Max(0f, value);
            ApplyMaterialProperties(CurrentIntensity);
        }
    }

    public bool Pulse
    {
        get => _pulse;
        set
        {
            _pulse = value;
            ApplyMaterialProperties(CurrentIntensity);
        }
    }

    private float CurrentIntensity
    {
        get
        {
            if (!_pulse || !Application.isPlaying)
            {
                return _glowIntensity;
            }

            float phase = Time.unscaledTime * Mathf.PI * 2f * _pulseFrequency;
            return Mathf.Max(0f, _glowIntensity + Mathf.Sin(phase) * _pulseAmount);
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        CacheImage();
        SetVerticesDirty();
        _image?.SetMaterialDirty();
    }

    private void Update()
    {
        if (_pulse && Application.isPlaying)
        {
            ApplyMaterialProperties(CurrentIntensity);
        }
    }

    protected override void OnDisable()
    {
        ReleaseRuntimeMaterial();
        SetVerticesDirty();
        _image?.SetMaterialDirty();
        base.OnDisable();
    }

    protected override void OnDestroy()
    {
        ReleaseRuntimeMaterial();
        base.OnDestroy();
    }

    protected override void OnDidApplyAnimationProperties()
    {
        base.OnDidApplyAnimationProperties();
        CacheImage();
        ApplyMaterialProperties(CurrentIntensity);
        _image?.SetMaterialDirty();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        _glowWidth = Mathf.Max(0f, _glowWidth);
        _glowIntensity = Mathf.Max(0f, _glowIntensity);
        _directionSamples = Mathf.Clamp(_directionSamples, 4, 16);
        _softnessRings = Mathf.Clamp(_softnessRings, 1, 4);
        _pulseAmount = Mathf.Max(0f, _pulseAmount);
        _pulseFrequency = Mathf.Max(0f, _pulseFrequency);

        CacheImage();
        ApplyMaterialProperties(CurrentIntensity);
        SetVerticesDirty();
        _image?.SetMaterialDirty();
    }
#endif

    public override void ModifyMesh(VertexHelper vertexHelper)
    {
        if (!IsActive() || vertexHelper == null || vertexHelper.currentVertCount == 0)
        {
            return;
        }

        _sourceVertices.Clear();
        vertexHelper.GetUIVertexStream(_sourceVertices);
        if (_sourceVertices.Count == 0)
        {
            return;
        }

        float maximumIntensity = _pulse ? _glowIntensity + _pulseAmount : _glowIntensity;
        if (_glowWidth <= 0f || _glowColor.a <= 0f || maximumIntensity <= 0f)
        {
            return;
        }

        int requestedCopies = _directionSamples * _softnessRings;
        int allowedCopies = Mathf.Max(0, MaxUiVertexCount / _sourceVertices.Count - 1);
        int copyCount = Mathf.Min(requestedCopies, allowedCopies);

        if (copyCount < requestedCopies && !_vertexLimitWarningLogged)
        {
            Debug.LogWarning(
                $"[{name}] Outer Glow 정점 수가 UI 한계를 넘어서 일부 Glow 샘플을 생략합니다.",
                this);
            _vertexLimitWarningLogged = true;
        }

        _outputVertices.Clear();
        _outputVertices.Capacity = Mathf.Max(
            _outputVertices.Capacity,
            _sourceVertices.Count * (copyCount + 1));

        int copiesWritten = 0;
        float sampleNormalization = 1f / Mathf.Sqrt(_directionSamples);

        // 바깥 링부터 그린 뒤 원본을 마지막에 그려 Glow가 이미지 내부를 덮지 않게 합니다.
        for (int ring = _softnessRings; ring >= 1 && copiesWritten < copyCount; ring--)
        {
            float ringRatio = ring / (float)_softnessRings;
            float radius = _glowWidth * ringRatio;
            float falloff = 1f - ((ring - 1f) / _softnessRings);
            float vertexAlpha = Mathf.Clamp01(falloff * sampleNormalization);
            float angleOffset = (ring & 1) == 0 ? Mathf.PI / _directionSamples : 0f;

            for (int direction = 0; direction < _directionSamples && copiesWritten < copyCount; direction++)
            {
                float angle = angleOffset + direction * Mathf.PI * 2f / _directionSamples;
                Vector3 offset = new(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                AppendGlowCopy(offset, vertexAlpha);
                copiesWritten++;
            }
        }

        for (int i = 0; i < _sourceVertices.Count; i++)
        {
            UIVertex original = _sourceVertices[i];
            Vector4 uv = original.uv0;
            uv.z = 0f;
            original.uv0 = uv;
            _outputVertices.Add(original);
        }

        vertexHelper.Clear();
        vertexHelper.AddUIVertexTriangleStream(_outputVertices);
    }

    public Material GetModifiedMaterial(Material baseMaterial)
    {
        if (!isActiveAndEnabled || baseMaterial == null)
        {
            return baseMaterial;
        }

        Shader shader = ResolveShader();
        if (shader == null)
        {
            if (!_missingShaderLogged)
            {
                Debug.LogError(
                    $"'{ShaderResourcePath}' UI Outer Glow 셰이더를 찾을 수 없습니다.",
                    this);
                _missingShaderLogged = true;
            }

            return baseMaterial;
        }

        if (_runtimeMaterial == null || _runtimeMaterial.shader != shader)
        {
            ReleaseRuntimeMaterial();
            _runtimeMaterial = new Material(shader)
            {
                name = $"{baseMaterial.name} (Outer Glow - {name})",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        _runtimeMaterial.CopyPropertiesFromMaterial(baseMaterial);
        ApplyMaterialProperties(CurrentIntensity);
        return _runtimeMaterial;
    }

    public void SetGlowColor(Color color)
    {
        GlowColor = color;
    }

    public void SetGlowWidth(float width)
    {
        GlowWidth = width;
    }

    public void SetGlowIntensity(float intensity)
    {
        GlowIntensity = intensity;
    }

    public void SetPulse(bool enabled)
    {
        Pulse = enabled;
    }

    public void Refresh()
    {
        CacheImage();
        SetVerticesDirty();
        _image?.SetMaterialDirty();
        ApplyMaterialProperties(CurrentIntensity);
    }

    private void AppendGlowCopy(Vector3 offset, float alpha)
    {
        for (int i = 0; i < _sourceVertices.Count; i++)
        {
            UIVertex glowVertex = _sourceVertices[i];
            glowVertex.position += offset;

            byte sourceAlpha = glowVertex.color.a;
            glowVertex.color = new Color32(
                255,
                255,
                255,
                (byte)Mathf.RoundToInt(sourceAlpha * alpha));

            Vector4 uv = glowVertex.uv0;
            uv.z = 1f;
            glowVertex.uv0 = uv;
            _outputVertices.Add(glowVertex);
        }
    }

    private void CacheImage()
    {
        if (_image == null)
        {
            _image = GetComponent<Image>();
        }
    }

    private void SetVerticesDirty()
    {
        CacheImage();
        _image?.SetVerticesDirty();
    }

    private Shader ResolveShader()
    {
        Shader shader = Resources.Load<Shader>(ShaderResourcePath);
        return shader != null ? shader : Shader.Find(ShaderFallbackName);
    }

    private void ApplyMaterialProperties(float intensity)
    {
        if (_runtimeMaterial == null)
        {
            return;
        }

        _runtimeMaterial.SetColor(GlowColorId, _glowColor);
        _runtimeMaterial.SetFloat(GlowIntensityId, Mathf.Max(0f, intensity));
    }

    private void ReleaseRuntimeMaterial()
    {
        if (_runtimeMaterial == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(_runtimeMaterial);
        }
        else
        {
            DestroyImmediate(_runtimeMaterial);
        }

        _runtimeMaterial = null;
    }
}
