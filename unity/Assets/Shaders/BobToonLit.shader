// Battle of the Brooms: BOB/ToonLit (Art Bible 21 section 5.1). A 3-step lit ramp (0.45 / 0.78 / 1.0, soft 0.08), a warm rim,
// a gradient ambient (sky blue above, violet below) and a dirt amount that greys and browns the colour. Built-in pipeline.
Shader "BOB/ToonLit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (palette)", 2D) = "white" {}
        _RampLow ("Ramp low", Range(0,1)) = 0.45
        _RampMid ("Ramp mid", Range(0,1)) = 0.78
        _RampSoft ("Ramp softness", Range(0.001,0.3)) = 0.08
        _RimColor ("Rim colour", Color) = (1,0.96,0.88,1)
        _Rim ("Rim strength", Range(0,1)) = 0.18
        _SkyColor ("Ambient sky", Color) = (0.867,0.922,0.98,1)
        _GroundColor ("Ambient ground", Color) = (0.56,0.525,0.75,1)
        _DirtAmount ("Dirt amount", Range(0,1)) = 0
        _DirtColor ("Dirt colour", Color) = (0.42,0.34,0.36,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            sampler2D _MainTex; float4 _MainTex_ST;
            fixed4 _Color, _RimColor, _SkyColor, _GroundColor, _DirtColor;
            float _RampLow, _RampMid, _RampSoft, _Rim, _DirtAmount;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 nrm : TEXCOORD1; float3 view : TEXCOORD2; fixed4 col : COLOR; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.nrm = UnityObjectToWorldNormal(v.normal);
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.view = normalize(_WorldSpaceCameraPos - wp);
                o.col = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 albedo = tex2D(_MainTex, i.uv) * _Color;
                albedo.rgb = lerp(albedo.rgb, albedo.rgb * 0.45 + _DirtColor.rgb * 0.55, _DirtAmount);
                float3 n = normalize(i.nrm);
                float ndl = dot(n, normalize(_WorldSpaceLightPos0.xyz)) * 0.5 + 0.5;
                float t = _RampLow;
                t = lerp(t, _RampMid, smoothstep(0.35 - _RampSoft, 0.35 + _RampSoft, ndl));
                t = lerp(t, 1.0, smoothstep(0.68 - _RampSoft, 0.68 + _RampSoft, ndl));
                float3 ambient = lerp(_GroundColor.rgb, _SkyColor.rgb, n.y * 0.5 + 0.5);
                float3 lit = lerp(ambient * 0.9, _LightColor0.rgb + ambient * 0.25, t);
                float rim = pow(1.0 - saturate(dot(n, i.view)), 3.0) * _Rim;
                fixed3 c = albedo.rgb * lit * lerp(1.0, i.col.r, 0.6) + _RimColor.rgb * rim * t;
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
