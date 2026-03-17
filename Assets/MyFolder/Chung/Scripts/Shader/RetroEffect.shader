Shader "Custom/RetroEffect"
{
    Properties
    {
        // Full Screen Pass가 자동으로 사용하는 소스 텍스처
        [HideInInspector] _BlitTexture ("Source", 2D) = "white" {}

        [Header(Base Retro)]
        _PixelSize      ("Pixel Size",        Float)        = 4.0
        _ScanlineCount  ("Scanline Count",    Float)        = 500.0
        _ScanlineOpacity("Scanline Opacity",  Range(0,0.3)) = 0.07
        _ScrollSpeed    ("Scroll Speed",      Float)        = 0.02

        [Header(CRT Distortion)]
        _BarrelStrength ("Barrel Strength",   Float)        = 0.15
        _SyncRollStr    ("Sync Roll",         Float)        = 0.015
        _ChromaStrength ("Chroma Split",      Float)        = 0.025
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off  Cull Off  ZTest Always

        Pass
        {
            Name "RetroEffect"

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // ── 머티리얼 프로퍼티 (인스펙터에서 조정) ──────────────────
            CBUFFER_START(UnityPerMaterial)
                float _PixelSize;
                float _ScanlineCount;
                float _ScanlineOpacity;
                float _ScrollSpeed;
                float _BarrelStrength;
                float _SyncRollStr;
                float _ChromaStrength;
            CBUFFER_END

            // ── 글로벌 유니폼 (Shader.SetGlobalFloat로 런타임 제어) ────
            // Properties 블록 밖에 선언해야 머티리얼 값에 덮이지 않음
            float _CRTIntensity;

            // ── 유틸 함수 ─────────────────────────────────────────────
            float2 BarrelDistort(float2 uv, float k)
            {
                float2 c = uv - 0.5;
                return uv + c * dot(c, c) * k;
            }

            float Hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            // ── 프래그먼트 셰이더 ──────────────────────────────────────
            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                // 1. 배럴 왜곡 (CRT 강도에 비례)
                float2 warpUV = lerp(uv, BarrelDistort(uv, _BarrelStrength), _CRTIntensity);

                // 2. 수평 싱크 롤 (브라운관 신호 끊김)
                warpUV.x += sin(warpUV.y * 80.0 + _Time.y * 12.0)
                            * _CRTIntensity * _SyncRollStr;

                // 3. 픽셀화 (왜곡된 UV 기준)
                float2 blocks = _ScreenParams.xy / _PixelSize;
                float2 pixUV  = floor(warpUV * blocks) / blocks;

                // 4. RGB 채널 분리 (항상 미세하게 + CRT 강도만큼 추가)
                float split = 0.002 + _ChromaStrength * _CRTIntensity;
                float r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, pixUV + float2( split, 0)).r;
                float g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, pixUV               ).g;
                float b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, pixUV - float2( split, 0)).b;
                half4 col = half4(r, g, b, 1.0);

                // 5. 스캔라인
                float scan = sin(uv.y * _ScanlineCount + _Time.y * _ScrollSpeed) * 0.5 + 0.5;
                col.rgb *= 1.0 - scan * _ScanlineOpacity;

                // 6. CRT 플리커
                col.rgb *= 1.0 + sin(_Time.y * 47.3) * _CRTIntensity * 0.04;

                // 7. CRT 정적 노이즈
                float noise = Hash21(uv + frac(_Time.y)) * _CRTIntensity * 0.08;
                col.rgb = saturate(col.rgb + noise);

                // 8. 비네트 (가장자리 어둡게)
                float2 vigC = warpUV - 0.5;
                float  vig  = saturate(1.0 - dot(vigC, vigC) * _CRTIntensity * 3.0);
                col.rgb *= vig;

                // 9. 배럴 왜곡으로 잘린 영역 블랙 처리
                if (_CRTIntensity > 0.01)
                {
                    if (warpUV.x < 0 || warpUV.x > 1 || warpUV.y < 0 || warpUV.y > 1)
                        col = half4(0, 0, 0, 1);
                }

                return col;
            }
            ENDHLSL
        }
    }
}
