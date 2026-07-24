Shader "UI/PrisonCollege/Backdrop Blur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Panel Mask", 2D) = "white" {}
        _Color ("Image Tint", Color) = (1, 1, 1, 1)
        _BackdropOpacity ("Backdrop Opacity", Range(0, 1)) = 1
        _OverlayColor ("Panel Overlay Color", Color) = (0.035, 0.055, 0.07, 0.35)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 screenPosition : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _UIBackdropBlurTexture;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            fixed4 _OverlayColor;
            float4 _ClipRect;
            half _BackdropOpacity;

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(output.worldPosition);
                output.screenPosition = ComputeScreenPos(output.vertex);
                output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 maskTexture = tex2D(_MainTex, input.texcoord) + _TextureSampleAdd;
                half maskAlpha = saturate(
                    maskTexture.a *
                    input.color.a *
                    _BackdropOpacity);

                float2 screenUv = input.screenPosition.xy /
                                  max(input.screenPosition.w, 0.0001);
                fixed3 backdrop = tex2D(_UIBackdropBlurTexture, screenUv).rgb;

                fixed3 imageTintedBackdrop = backdrop * input.color.rgb;
                fixed3 panelColor = lerp(
                    imageTintedBackdrop,
                    _OverlayColor.rgb,
                    saturate(_OverlayColor.a));

                fixed4 color = fixed4(panelColor, maskAlpha);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001h);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
