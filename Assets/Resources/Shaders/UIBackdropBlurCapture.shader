Shader "Hidden/PrisonCollege/UI Backdrop Blur Capture"
{
    Properties
    {
        _BlurRadius ("Blur Radius", Float) = 1.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "Kawase Blur"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _BlurRadius;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord.xy;
                float2 offset = _BlitTexture_TexelSize.xy *
                                max(_BlurRadius, 0.0);

                half4 color = 0.0h;
                color += SAMPLE_TEXTURE2D_X_LOD(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv + float2(-offset.x, -offset.y),
                    _BlitMipLevel);
                color += SAMPLE_TEXTURE2D_X_LOD(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv + float2( offset.x, -offset.y),
                    _BlitMipLevel);
                color += SAMPLE_TEXTURE2D_X_LOD(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv + float2(-offset.x,  offset.y),
                    _BlitMipLevel);
                color += SAMPLE_TEXTURE2D_X_LOD(
                    _BlitTexture,
                    sampler_LinearClamp,
                    uv + float2( offset.x,  offset.y),
                    _BlitMipLevel);

                return color * 0.25h;
            }
            ENDHLSL
        }
    }
}
