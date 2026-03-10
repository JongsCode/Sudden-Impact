Shader "Custom/WeaponNodeShader"
{
    Properties {
        _MainTex ("Weapon Texture", 2D) = "white" {} 
        _Color ("Base Color", Color) = (1,1,1,1)
        [HDR] _NeonColor ("Neon Outline Color", Color) = (0, 1, 1, 1) // Neon Cyan
        _FresnelPower ("Outline Width", Range(0.1, 10.0)) = 3.0
    }
    SubShader {
        Tags { "RenderType" = "Opaque" } 
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc" 

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL; // 프레넬 연산을 위해 추가
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : NORMAL; // 월드 노멀 전달
                float3 viewDir : TEXCOORD3;  // 시선 방향 전달
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _NeonColor;
            float _FresnelPower;
            
            sampler2D _GlobalCurrentMap; 
            float4 _MapParams; 

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(_WorldSpaceCameraPos.xyz - o.worldPos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // 1. 기존 안개 로직 (팀원분 코드 유지)
                float2 mapCenter = _MapParams.xy;
                float mapSize = _MapParams.z;
                float2 fogUV = (i.worldPos.xz - mapCenter) / mapSize + 0.5;
                float visibility = tex2D(_GlobalCurrentMap, fogUV).r;

                // 2. 기본 텍스처 컬러
                fixed4 baseColor = tex2D(_MainTex, i.uv) * _Color;

                // 3. 네온 프레넬 로직 추가 (테두리 계산)
                float fresnel = pow(1.0 - saturate(dot(normalize(i.worldNormal), i.viewDir)), _FresnelPower);
                fixed3 neonEmission = fresnel * _NeonColor.rgb;

                // 4. 안개 적용 (안개 밖이면 투명하게 날림)
                if (visibility < 0.1) discard; 

                // 5. 최종 출력: 기본 컬러 + 네온 테두리
                return fixed4(baseColor.rgb + neonEmission, baseColor.a);
            }
            ENDCG
        }
    }
}