Shader "Custom/NeonBlood"
{
    Properties {
        _MainTex ("Blood Splatter Texture", 2D) = "white" {} 
        
        // [HDR] 태그를 붙이면 인스펙터에서 빛의 강도(Intensity)를 1 이상으로 설정
        [HDR] _Color ("Neon Color", Color) = (1, 0, 0.5, 1) 
        
        // 직관적으로 밝기를 조절할 수 있는 곱하기 변수 추가
        _EmissionGlow ("Glow Multiplier", Range(1, 10)) = 2.0 
    }
    SubShader {
        // Opaque(불투명)였던 설정을 Transparent(투명)로 변경하고, 바닥 위에 그려지도록 설정
        Tags { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
        } 
        
        // 배경이 투명하게 블렌딩 모드 켜기 & ZWrite 끄기
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc" 

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _Color;
            float _EmissionGlow;
            
            // 기존 전장의 안개(Fog) 변수 유지
            sampler2D _GlobalCurrentMap; 
            float4 _MapParams;

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // 하얀색 투명 혈흔 스프라이트 읽기
                fixed4 texColor = tex2D(_MainTex, i.uv);
                
                // 텍스처 컬러에 HDR 네온 색상과 Glow 배수를 곱해서 밝게 만듦 
                fixed4 finalColor = texColor * _Color * i.color * _EmissionGlow;                
                // 투명도(Alpha)는 텍스처 원본의 투명도와 컬러의 투명도를 곱해서 유지
                finalColor.a = texColor.a * _Color.a;

                // 기존 시야(Fog) 시스템 유지: 안개 속에 있으면 피도 안 보이게 
                float2 mapCenter = _MapParams.xy;
                float mapSize = _MapParams.z;
                float2 fogUV = (i.worldPos.xz - mapCenter) / mapSize + 0.5;
                fixed currentValue = tex2D(_GlobalCurrentMap, fogUV).r;
                
                clip(currentValue - 0.01); 

                return finalColor;
            }
            ENDCG
        }
    }
}