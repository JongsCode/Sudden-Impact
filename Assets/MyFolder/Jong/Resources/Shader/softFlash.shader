Shader "Custom/SoftFlash"
{
    Properties
    {
        [MainColor] _LightColor("Light Color", Color) = (1, 1, 1, 1) 
        _Intensity ("Light Intensity", Range(0, 10)) = 2.0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }
        
        BlendOp Max
        Blend One One 
        
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;             
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _LightColor;
                float _Intensity;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.color = IN.color; 
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
               
                
                float finalAlpha = _LightColor.a * IN.color.a;
                float3 finalColor = _LightColor.rgb * _Intensity * finalAlpha;

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}