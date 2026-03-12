Shader "Custom/UniversalNeon"
{
    Properties {
        _MainTex ("Texture (Sprite/Trail/Bullet)", 2D) = "white" {} 
        [HDR] _Color ("Material Color", Color) = (1, 1, 1, 1) 
        _EmissionGlow ("Glow Multiplier", Range(1, 10)) = 2.0 
        
        [Space(10)]
        [Header(Render Settings)]
        // 인스펙터에서 블렌딩 모드와 ZWrite를 드롭다운으로 선택할 수 있게 뺍니다!
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Float) = 5 // 기본값: SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend", Float) = 10 // 기본값: OneMinusSrcAlpha
        [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Float) = 0 // 기본값: Off
    }
    SubShader {
        // 총알(불투명)이라도 발광체(Neon)는 Opaque 뒤에 그려지는 Transparent 큐에 두는 것이 
        // 렌더링 순서상 (이펙트가 배경에 묻히지 않게) 훨씬 안전하고 예쁩니다!
        Tags { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "IgnoreProjector"="True" 
            "PreviewType"="Plane" 
        } 
        
        // 인스펙터에서 선택한 값을 쉐이더 설정으로 적용
        ZWrite [_ZWrite]
        Blend [_SrcBlend] [_DstBlend]

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
                fixed4 texColor = tex2D(_MainTex, i.uv);
                
                fixed4 finalColor = texColor * _Color * i.color * _EmissionGlow;
                finalColor.a = texColor.a * _Color.a * i.color.a;

                // 기존 시야(Fog) 시스템 유지
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