Shader "Custom/FloorShader"
{
    Properties
    {
        _MainTex ("Floor Texture", 2D) = "white" {} // 바닥에 입힐 원래 이미지/텍스처
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1; 
            };

            sampler2D _MainTex;
            
            sampler2D _GlobalMap;
            sampler2D _GlobalCurrentMap;
            float4 _MapParams;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 바닥의 원래 색상
                fixed4 baseColor = tex2D(_MainTex, i.uv);

								// Params을 이용하여 지도상의 UV 좌표로 변환
                float2 mapCenter = _MapParams.xy;
                float mapSize = _MapParams.z;
                float2 fogUV = (i.worldPos.xz - mapCenter) / mapSize + 0.5;

                // 전역 텍스처에서 UV에 해당하는 값을 변수로 저장
                fixed overlapValue = tex2D(_GlobalMap, fogUV).r;
                fixed currentValue = tex2D(_GlobalCurrentMap, fogUV).r;

                fixed finalValue = max(overlapValue * 0.3, currentValue);
                return baseColor * finalValue;
                //return fixed4(overlapValue, overlapValue, overlapValue, 1.0);
                //return baseColor * fogValue;
            }
            ENDCG
        }
    }
}