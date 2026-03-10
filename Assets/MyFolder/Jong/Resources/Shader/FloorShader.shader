Shader "Custom/FogOfWarOverlay"
{
    Properties
    {
        _VisitedAlpha ("방문한 곳 투명도 조절", Range(0, 1)) = 0.75
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off 

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD1; 
            };

            fixed _VisitedAlpha;

            sampler2D _GlobalMap;
            sampler2D _GlobalCurrentMap;
            //sampler2D _GlobalTempMap;

            float4 _MapParams;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 mapCenter = _MapParams.xy;
                float mapSize = _MapParams.z;
                float2 fogUV = (i.worldPos.xz - mapCenter) / mapSize + 0.5;

                fixed overlapValue = tex2D(_GlobalMap, fogUV).r;     
                fixed currentValue = tex2D(_GlobalCurrentMap, fogUV).r; 
              //  fixed tempValue = tex2D(_GlobalTempMap, fogUV).r;       

              //  fixed finalCurrent = max(currentValue, tempValue);

                
                fixed finalAlpha = 1.0; 

                finalAlpha = lerp(finalAlpha, _VisitedAlpha, overlapValue);

                finalAlpha = lerp(finalAlpha, 0.0, currentValue);

                return fixed4(0.0, 0.0, 0.0, finalAlpha);
            }
            ENDCG
        }
    }
}