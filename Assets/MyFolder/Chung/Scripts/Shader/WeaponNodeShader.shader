Shader "Custom/WeaponNodeShader"
{
    Properties {
        [HDR] _NeonColor ("Neon Outline Color", Color) = (0, 1, 1, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.02
    }
    SubShader {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+1" }

        // Pass 1: 무기 실루엣을 스텐실에 기록 (화면 출력 없음)
        Pass {
            Name "StencilWrite"
            Cull Back
            ZWrite Off
            ZTest LEqual
            ColorMask 0

            Stencil {
                Ref 2
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            sampler2D _GlobalCurrentMap;
            float4 _MapParams;

            Varyings vert(Attributes IN) {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target {
                float2 fogUV = (IN.worldPos.xz - _MapParams.xy) / _MapParams.z + 0.5;
                if (tex2D(_GlobalCurrentMap, fogUV).r < 0.01) discard;
                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }

        // Pass 2: 스텐실 2 바깥에만 네온 아웃라인 출력
        Pass {
            Name "OutlineRender"
            Cull Back
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            Stencil {
                Ref 2
                Comp NotEqual
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            half4 _NeonColor;
            float _OutlineWidth;
            sampler2D _GlobalCurrentMap;
            float4 _MapParams;

            Varyings vert(Attributes IN) {
                Varyings OUT;
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);

                float4 clipPos = TransformObjectToHClip(IN.positionOS.xyz);
                float4 clipCenter = TransformObjectToHClip(float3(0, 0, 0));

                // NDC 공간에서 중심 기준 방향 계산 후 클립 공간으로 적용
                float2 ndcPos = clipPos.xy / clipPos.w;
                float2 ndcCenter = clipCenter.xy / clipCenter.w;
                float2 dir = normalize(ndcPos - ndcCenter + float2(0.0001, 0.0001));
                clipPos.xy += dir * _OutlineWidth * clipPos.w;

                OUT.positionHCS = clipPos;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target {
                float2 fogUV = (IN.worldPos.xz - _MapParams.xy) / _MapParams.z + 0.5;
                if (tex2D(_GlobalCurrentMap, fogUV).r < 0.01) discard;
                return half4(_NeonColor.rgb, _NeonColor.a);
            }
            ENDHLSL
        }
    }
}
