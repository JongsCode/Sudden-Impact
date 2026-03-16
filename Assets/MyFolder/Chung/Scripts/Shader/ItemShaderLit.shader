Shader "Custom/ItemShaderLit"
{
    Properties
    {
        _MainTex    ("Albedo (RGB)", 2D)      = "white" {}
        _Color      ("Tint Color", Color)      = (1,1,1,1)
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _Metallic   ("Metallic",   Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }

        // 式式 Pass 1: Forward Lit 式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // FOW 旋煎弊 (ItemShader 翕橾)
            TEXTURE2D(_GlobalCurrentMap); SAMPLER(sampler_GlobalCurrentMap);
            TEXTURE2D(_GlobalMap);        SAMPLER(sampler_GlobalMap);
            float4 _MapParams;

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float  _Smoothness;
                float  _Metallic;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);
                float3 positionWS : TEXCOORD2;
                float3 normalWS   : TEXCOORD3;
                float  fogFactor  : TEXCOORD4;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                VertexPositionInputs posInputs  = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS   = normInputs.normalWS;
                output.uv         = TRANSFORM_TEX(input.uv, _MainTex);
                output.fogFactor  = ComputeFogFactor(posInputs.positionCS.z);

                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS, output.vertexSH);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 式式 FOW 陛衛撩 式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式
                float2 fogUV = (input.positionWS.xz - _MapParams.xy) / _MapParams.z + 0.5;

                float currentValue = SAMPLE_TEXTURE2D(_GlobalCurrentMap, sampler_GlobalCurrentMap, fogUV).r;
                float overlapValue = SAMPLE_TEXTURE2D(_GlobalMap,        sampler_GlobalMap,        fogUV).r;

                clip(overlapValue - 0.1);
                float fowBrightness = max(currentValue, 0.3);

                // 式式 Albedo 式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;

                float3 normalWS = normalize(input.normalWS);

                // 式式 InputData 式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式
                InputData lightData;
                ZERO_INITIALIZE(InputData, lightData);
                lightData.positionWS             = input.positionWS;
                lightData.normalWS               = normalWS;
                lightData.viewDirectionWS        = GetWorldSpaceNormalizeViewDir(input.positionWS);
                lightData.bakedGI                = SAMPLE_GI(input.lightmapUV, input.vertexSH, normalWS);
                lightData.shadowMask             = SAMPLE_SHADOWMASK(input.lightmapUV);
                lightData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                lightData.fogCoord               = input.fogFactor;

                // 式式 SurfaceData 式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式
                SurfaceData surfData;
                ZERO_INITIALIZE(SurfaceData, surfData);
                surfData.albedo     = albedo.rgb;
                surfData.alpha      = albedo.a;
                surfData.metallic   = _Metallic;
                surfData.smoothness = _Smoothness;
                surfData.normalTS   = half3(0, 0, 1);
                surfData.occlusion  = 1.0;

                half4 color = UniversalFragmentPBR(lightData, surfData);

                // 式式 FOW 嫩晦 + ん斜 式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式
                color.rgb *= fowBrightness;
                color.rgb  = MixFog(color.rgb, input.fogFactor);

                return color;
            }
            ENDHLSL
        }

        // 式式 Pass 2: Shadow Caster 式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex   ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // 式式 Pass 3: DepthOnly 式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex   DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        // 式式 Pass 4: Meta (塭檜お裘 漆檜韁) 式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式
        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex   vert_meta
            #pragma fragment frag_meta

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float  _Smoothness;
                float  _Metallic;
            CBUFFER_END

            struct Attributes_Meta
            {
                float4 positionOS : POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
            };

            struct Varyings_Meta
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings_Meta vert_meta(Attributes_Meta input)
            {
                Varyings_Meta output;
                output.positionCS = MetaVertexPosition(
                    input.positionOS, input.uv1, input.uv2,
                    unity_LightmapST, unity_DynamicLightmapST);
                output.uv = TRANSFORM_TEX(input.uv0, _MainTex);
                return output;
            }

            half4 frag_meta(Varyings_Meta input) : SV_Target
            {
                MetaInput metaInput = (MetaInput)0;
                metaInput.Albedo   = (SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color).rgb;
                metaInput.Emission = half3(0, 0, 0);
                return MetaFragment(metaInput);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
