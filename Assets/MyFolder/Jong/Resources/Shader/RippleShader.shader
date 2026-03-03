Shader "Custom/Ripple"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Progress ("Progress", Range(0, 1)) = 0.0 
        _Thickness ("Thickness", Range(1, 20)) = 8.0 
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        
        
        Blend SrcAlpha OneMinusSrcAlpha 
        ZWrite Off

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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _Color;
            float _Progress;
            float _Thickness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float dist = distance(i.uv, float2(0.5, 0.5));
                dist = dist * 2.0;

                float ring = 1.0 - saturate(abs(dist - _Progress) * _Thickness);

                float fadeOut = 1.0 - _Progress;

                return float4(_Color.rgb, ring * fadeOut * _Color.a);
            }
            ENDCG
        }
    }
}