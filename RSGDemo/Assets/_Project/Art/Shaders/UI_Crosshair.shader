Shader "UI/Crosshair"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OuterRadius ("Outer Radius", Range(0, 1)) = 0.35
        _InnerRadius ("Inner Radius", Range(0, 1)) = 0
        _OutlineColor ("Outline Color", Color) = (0,0,0,0.45)
        _OutlineWidth ("Outline Width", Range(0, 0.3)) = 0.08
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OuterRadius;
            float _InnerRadius;
            float _OutlineWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            float ring (float distance, float inner, float outer, float aa)
            {
                float outside = smoothstep(outer + aa, outer - aa, distance);
                float hole = inner > 0 ? smoothstep(inner - aa, inner + aa, distance) : 1;
                return outside * hole;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float distance = length(i.uv * 2 - 1);
                float aa = fwidth(distance);

                float fill = ring(distance, _InnerRadius, _OuterRadius, aa);
                float outlineInner = max(0, _InnerRadius - _OutlineWidth * step(0.0001, _InnerRadius));
                float outline = ring(distance, outlineInner, _OuterRadius + _OutlineWidth, aa);

                fixed4 color = lerp(_OutlineColor, i.color, fill);
                color.a = lerp(_OutlineColor.a * outline, i.color.a, fill);
                return color;
            }
            ENDCG
        }
    }
}
