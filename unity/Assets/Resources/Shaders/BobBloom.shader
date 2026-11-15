// Battle of the Brooms: the cheap bloom behind BobPostGrade (docs 24 section 15.15). The project is on the built-in
// pipeline (no URP), so this replaces a post-processing volume: one half-resolution bright pass plus two 5-tap blur
// passes. Three quarter-screen draws per frame, no compute, no extra camera.
Shader "Hidden/BOB/Bloom"
{
    Properties { _MainTex ("", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // 0: bright pass. Keeps only what is brighter than the threshold, so clean surfaces stay clean.
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Threshold;

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
                fixed3 c = tex2D(_MainTex, i.uv).rgb;
                float l = dot(c, float3(0.299, 0.587, 0.114));
                float k = saturate((l - _Threshold) / max(0.0001, 1.0 - _Threshold));
                return fixed4(c * k, 1);
            }
            ENDCG
        }

        // 1: separable blur. _BlurDir is already scaled to texels.
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float2 _BlurDir;

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
                float2 d = _BlurDir;
                fixed3 c = tex2D(_MainTex, i.uv).rgb * 0.4;
                c += tex2D(_MainTex, i.uv + d).rgb * 0.15;
                c += tex2D(_MainTex, i.uv - d).rgb * 0.15;
                c += tex2D(_MainTex, i.uv + d * 2.5).rgb * 0.15;
                c += tex2D(_MainTex, i.uv - d * 2.5).rgb * 0.15;
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
}
