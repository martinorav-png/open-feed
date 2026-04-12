Shader "OPENFEED/MainMenu CRT Feed"
{
    Properties
    {
        [PerRendererData] _MainTex ("Feed", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _StaticIntensity ("Static", Range(0, 0.35)) = 0.1
        _RollShift ("Roll Shift", Float) = 0
        _NoiseSeed ("Noise Seed", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Off
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
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            half _StaticIntensity;
            float _RollShift;
            float _NoiseSeed;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            float nrand(float2 co)
            {
                return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                uv.x += _RollShift;

                float t = _Time.y + _NoiseSeed;
                float band = floor(uv.y * 140.0);
                float tear = step(0.92, nrand(float2(band, floor(t * 12.0))));
                uv.x += (nrand(float2(band * 3.17, t * 7.3)) - 0.5) * 0.06 * tear;

                fixed4 col = tex2D(_MainTex, uv) * i.color;

                float2 nuv = uv * float2(512.0, 512.0) + float2(t * 73.1, t * -51.7);
                float snow = nrand(nuv) * nrand(nuv * 1.7 + 19.3);
                float snow2 = nrand(nuv * 3.1 + float2(sin(t * 8.0), cos(t * 5.0)));
                col.rgb += (snow * 0.55 + snow2 * 0.45) * _StaticIntensity;

                return col;
            }
            ENDCG
        }
    }
    FallBack "UI/Default"
}
