Shader "Custom/FOWShader"
{
    Properties {
        _Color ("Fog Color", Color) = (0, 0, 0, 1)
        _MainTex ("FowCurrent (RGB)", 2D) = "white" {}
        _MainTex2 ("FowOverlap (RGB)", 2D) = "white" {}
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off 
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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _MainTex2;
            fixed4 _Color;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex); 
                o.uv = v.uv;
                return o;
            }

                fixed4 frag (v2f i) : SV_Target {
                fixed4 colorCurrent = tex2D(_MainTex, i.uv);
                fixed4 colorOverlap = tex2D(_MainTex2, i.uv);

                float alpha = 1.0 - ceil(colorOverlap.r) * 0.2f;

                return fixed4(_Color.rgb, alpha * _Color.a);
            }
            ENDCG
        }
    }
}