Shader "UI/SpinnerCircle"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Thickness ("Thickness", Range(0,1)) = 0.15
        _Speed ("Rotation Speed", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Thickness;
            float _Speed;

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * 2.0 - 1.0; 
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float dist = length(uv);
                if (dist > 1 || dist < (1.0 - _Thickness))
                    return 0;

                float angle = atan2(uv.y, uv.x) / 6.2832; 
                angle = frac(angle + _Time * _Speed);

                float arc = step(angle, 0.25);

                return _Color * arc;
            }
            ENDCG
        }
    }
}