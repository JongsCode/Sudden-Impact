Shader "Custom/WeaponNodeIndicator"
{
    Properties {
        [HDR] _NeonColor ("Indicator Color", Color) = (0, 1, 1, 1)
        _InnerRadius ("Inner Radius", Range(0, 0.5)) = 0.35
        _OuterRadius ("Outer Radius", Range(0, 0.5)) = 0.48
        _Softness ("Edge Softness", Range(0.001, 0.1)) = 0.02
    }
    SubShader {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _NeonColor;
                float _InnerRadius;
                float _OuterRadius;
                float _Softness;
            CBUFFER_END

            sampler2D _GlobalCurrentMap;
            float4 _MapParams;

            Varyings vert(Attributes IN) {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target {
                // 시야 밖이면 숨김
                float2 fogUV = (IN.worldPos.xz - _MapParams.xy) / _MapParams.z + 0.5;
                if (tex2D(_GlobalCurrentMap, fogUV).r < 0.01) discard;

                // UV 중심 기준 거리로 링 마스크
                float dist = length(IN.uv - 0.5);
                float outer = smoothstep(_OuterRadius, _OuterRadius - _Softness, dist);
                float inner = smoothstep(_InnerRadius, _InnerRadius + _Softness, dist);
                float ring = outer * inner;

                if (ring < 0.001) discard;
                return half4(_NeonColor.rgb, _NeonColor.a * ring);
            }
            ENDHLSL
        }
    }
}
