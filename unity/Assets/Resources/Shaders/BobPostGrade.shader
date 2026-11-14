// Battle of the Brooms: a light photographic grade for the Task Race scenes (docs 24). Built-in pipeline image effect:
// a soft vignette, a little contrast and saturation, and a cool-shadow / warm-highlight split tone. It is deliberately
// mild — the palette is the art direction; this just stops the render reading as raw engine output.
Shader "Hidden/BOB/PostGrade"
{
    Properties
    {
        _MainTex ("", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Vignette, _Saturation, _Contrast;
            float4 _ShadowTone, _HighlightTone;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);

                c.rgb = saturate((c.rgb - 0.5) * _Contrast + 0.5);
                float lum = dot(c.rgb, float3(0.299, 0.587, 0.114));
                c.rgb = lerp(lum.xxx, c.rgb, _Saturation);

                float l = saturate((lum - 0.25) * 1.6);
                c.rgb *= lerp(_ShadowTone.rgb, _HighlightTone.rgb, l);

                float2 d = i.uv - 0.5;
                c.rgb *= saturate(1.0 - dot(d, d) * _Vignette);
                return c;
            }
            ENDCG
        }
    }
}
