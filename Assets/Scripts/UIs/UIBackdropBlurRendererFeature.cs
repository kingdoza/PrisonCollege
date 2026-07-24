using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public sealed class UIBackdropBlurRendererFeature : ScriptableRendererFeature
{
    private const string CaptureShaderResourcePath = "Shaders/UIBackdropBlurCapture";
    private const string CaptureShaderFallbackName = "Hidden/PrisonCollege/UI Backdrop Blur Capture";

    [Serializable]
    public sealed class Settings
    {
        [Tooltip("카메라 화면을 캡처할 시점입니다. UI보다 먼저 실행되는 AfterRenderingPostProcessing을 권장합니다.")]
        public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;

        [Range(1, 4)]
        [Tooltip("캡처 해상도의 축소 배율입니다. 2는 화면 가로/세로를 각각 절반으로 줄입니다.")]
        public int downsample = 2;

        [Range(0.5f, 4f)]
        [Tooltip("각 블러 패스의 샘플 간격입니다.")]
        public float blurRadius = 1.5f;

        [Range(1, 4)]
        [Tooltip("공유 캡처 텍스처에 적용할 블러 반복 횟수입니다.")]
        public int blurIterations = 2;

        [Tooltip("Scene View에서도 배경 블러 캡처를 실행합니다.")]
        public bool includeSceneView;
    }

    [SerializeField]
    private Settings _settings = new();

    private UIBackdropBlurPass _pass;
    private Material _captureMaterial;
    private bool _missingShaderLogged;

    public override void Create()
    {
        ReleaseResources();

        Shader captureShader = Resources.Load<Shader>(CaptureShaderResourcePath);
        if (captureShader == null)
        {
            captureShader = Shader.Find(CaptureShaderFallbackName);
        }

        if (captureShader == null)
        {
            if (!_missingShaderLogged)
            {
                Debug.LogError(
                    $"UI 배경 블러 캡처 셰이더 '{CaptureShaderResourcePath}'를 찾을 수 없습니다.",
                    this);
                _missingShaderLogged = true;
            }

            return;
        }

        _missingShaderLogged = false;
        _captureMaterial = CoreUtils.CreateEngineMaterial(captureShader);
        _pass = new UIBackdropBlurPass(_captureMaterial)
        {
            renderPassEvent = _settings.injectionPoint
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_pass == null || _captureMaterial == null)
        {
            return;
        }

        CameraData cameraData = renderingData.cameraData;
        Camera camera = cameraData.camera;
        if (camera == null || cameraData.renderType != CameraRenderType.Base)
        {
            return;
        }

        if (camera.cameraType == CameraType.SceneView)
        {
            if (!_settings.includeSceneView)
            {
                return;
            }
        }
        else if (camera.cameraType != CameraType.Game)
        {
            return;
        }

        _pass.renderPassEvent = _settings.injectionPoint;
        _pass.Setup(cameraData.cameraTargetDescriptor, _settings);
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        ReleaseResources();
        base.Dispose(disposing);
    }

    private void ReleaseResources()
    {
        _pass?.Dispose();
        _pass = null;

        CoreUtils.Destroy(_captureMaterial);
        _captureMaterial = null;
    }

    private sealed class UIBackdropBlurPass : ScriptableRenderPass
    {
        private const string PassName = "UI Backdrop Blur Capture";
        private const string GlobalTextureName = "_UIBackdropBlurTexture";
        private const string BlurTextureAName = "_UIBackdropBlurA";
        private const string BlurTextureBName = "_UIBackdropBlurB";

        private static readonly int GlobalTextureId = Shader.PropertyToID(GlobalTextureName);
        private static readonly int BlurRadiusId = Shader.PropertyToID("_BlurRadius");

        private readonly Material _material;
        private readonly ProfilingSampler _profilingSampler = new(PassName);

        private RTHandle _blurTextureA;
        private RTHandle _blurTextureB;
        private int _blurIterations = 2;
        private bool _backBufferWarningLogged;

        public UIBackdropBlurPass(Material material)
        {
            _material = material;
            requiresIntermediateTexture = true;
        }

        public void Setup(RenderTextureDescriptor cameraDescriptor, Settings settings)
        {
            int downsample = Mathf.Clamp(settings.downsample, 1, 4);
            _blurIterations = Mathf.Clamp(settings.blurIterations, 1, 4);
            _material.SetFloat(BlurRadiusId, Mathf.Max(0.5f, settings.blurRadius));

            RenderTextureDescriptor descriptor = cameraDescriptor;
            descriptor.width = Mathf.Max(1, cameraDescriptor.width / downsample);
            descriptor.height = Mathf.Max(1, cameraDescriptor.height / downsample);
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            descriptor.bindMS = false;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.enableRandomWrite = false;
            descriptor.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;

            RenderingUtils.ReAllocateHandleIfNeeded(
                ref _blurTextureA,
                descriptor,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: BlurTextureAName);

            RenderingUtils.ReAllocateHandleIfNeeded(
                ref _blurTextureB,
                descriptor,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: BlurTextureBName);
        }

#pragma warning disable 618, 672
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null || _blurTextureA == null || _blurTextureB == null)
            {
                return;
            }

            RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            CommandBuffer commandBuffer = CommandBufferPool.Get(PassName);

            using (new ProfilingScope(commandBuffer, _profilingSampler))
            {
                for (int iteration = 0; iteration < _blurIterations; iteration++)
                {
                    RTHandle destination = (iteration & 1) == 0
                        ? _blurTextureA
                        : _blurTextureB;

                    Blitter.BlitCameraTexture(commandBuffer, source, destination, _material, 0);
                    source = destination;
                }

                commandBuffer.SetGlobalTexture(GlobalTextureId, source.nameID);
            }

            context.ExecuteCommandBuffer(commandBuffer);
            CommandBufferPool.Release(commandBuffer);
        }
#pragma warning restore 618, 672

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_material == null || _blurTextureA == null || _blurTextureB == null)
            {
                return;
            }

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer)
            {
                if (!_backBufferWarningLogged)
                {
                    Debug.LogWarning(
                        "UI 배경 블러가 BackBuffer를 직접 읽을 수 없어 이번 캡처를 건너뜁니다. " +
                        "Renderer Feature의 Injection Point를 AfterRenderingPostProcessing 이전으로 설정하세요.");
                    _backBufferWarningLogged = true;
                }

                return;
            }

            _backBufferWarningLogged = false;

            TextureHandle textureA = renderGraph.ImportTexture(_blurTextureA);
            TextureHandle textureB = renderGraph.ImportTexture(_blurTextureB);
            TextureHandle source = resourceData.activeColorTexture;

            if (!source.IsValid() || !textureA.IsValid() || !textureB.IsValid())
            {
                return;
            }

            for (int iteration = 0; iteration < _blurIterations; iteration++)
            {
                TextureHandle destination = (iteration & 1) == 0 ? textureA : textureB;
                RenderGraphUtils.BlitMaterialParameters parameters =
                    new(source, destination, _material, 0);

                renderGraph.AddBlitPass(
                    parameters,
                    passName: $"{PassName} {iteration + 1}");

                source = destination;
            }

            using (IRasterRenderGraphBuilder builder =
                   renderGraph.AddRasterRenderPass<SetGlobalTexturePassData>(
                       "Set UI Backdrop Blur Texture",
                       out _))
            {
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetGlobalTextureAfterPass(source, GlobalTextureId);
                builder.SetRenderFunc(
                    static (SetGlobalTexturePassData _, RasterGraphContext _) => { });
            }
        }

        public void Dispose()
        {
            _blurTextureA?.Release();
            _blurTextureA = null;

            _blurTextureB?.Release();
            _blurTextureB = null;
        }

        private sealed class SetGlobalTexturePassData
        {
        }
    }
}
