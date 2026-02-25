Shader "Custom/ItemShader"
{
    Properties {
        _MainTex ("Item Texture", 2D) = "white" {} 
        _Color ("Tint Color", Color) = (1,1,1,1)
    }
    SubShader {
        Tags { "RenderType"="Opaque" } 
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc" 

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1; 
            };

            sampler2D _MainTex;
            fixed4 _Color;
            
            
            sampler2D _GlobalCurrentMap; 
            sampler2D _GlobalMap;        
            float4 _MapParams; 

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 baseColor = tex2D(_MainTex, i.uv) * _Color;

                float2 mapCenter = _MapParams.xy;
                float mapSize = _MapParams.z; 
                float2 fogUV = (i.worldPos.xz - mapCenter) / mapSize + 0.5;

                
                fixed currentValue = tex2D(_GlobalCurrentMap, fogUV).r; 
                fixed overlapValue = tex2D(_GlobalMap, fogUV).r;        

                
                clip(overlapValue - 0.1); 

                
                float brightness = max(currentValue, 0.3); 
                
                return baseColor * brightness;
            }
            ENDCG
        }
    }
}