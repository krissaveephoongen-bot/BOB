// Battle of the Brooms: BOB/ToonLit (Art Bible 21 section 5.1). A 3-step lit ramp (0.45 / 0.78 / 1.0, soft 0.08), a warm rim,
// the scene's trilight ambient, real shadow receiving, a tight toon sheen and a dirt amount that greys and browns the colour.
// Built-in pipeline; the shadow caster comes from the Diffuse fallback (flat geometry, so no custom caster is needed).
Shader "BOB/ToonLit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (palette)", 2D) = "white" {}
        _RampLow ("Ramp low", Range(0,1)) = 0.45
        _RampMid ("Ramp mid", Range(0,1)) = 0.78
        _RampSoft ("Ramp softness", Range(0.001,0.3)) = 0.08
        _ShadowTint ("Shadow tint (share of key light)", Color) = (0.55,0.58,0.78,1)
        _AmbientBoost ("Scene ambient boost", Range(0,2)) = 1
        _SheenColor ("Sheen colour", Color) = (1,1,1,1)
        _Shininess ("Sheen tightness", Range(1,128)) = 32
        _Sheen ("Sheen strength", Range(0,0.5)) = 0.10
        _RimColor ("Rim colour", Color) = (1,0.96,0.88,1)
        _Rim ("Rim strength", Range(0,1)) = 0.18
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
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            sampler2D _MainTex; float4 _MainTex_ST;
            fixed4 _Color, _RimColor, _SheenColor, _ShadowTint, _DirtColor;
            float _RampLow, _RampMid, _RampSoft, _Rim, _DirtAmount;
            float _AmbientBoost, _Shininess, _Sheen;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 nrm : TEXCOORD1;
                float3 view : TEXCOORD2;
                SHADOW_COORDS(3)
                UNITY_FOG_COORDS(4)
                fixed4 col : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.nrm = UnityObjectToWorldNormal(v.normal);
                o.view = normalize(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex).xyz);
                o.col = v.color;
                TRANSFER_SHADOW(o);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 albedo = tex2D(_MainTex, i.uv) * _Color;
                albedo.rgb = lerp(albedo.rgb, albedo.rgb * 0.45 + _DirtColor.rgb * 0.55, _DirtAmount);
                float3 n = normalize(i.nrm);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);

                float ndl = dot(n, lightDir) * 0.5 + 0.5;
                float t = _RampLow;
                t = lerp(t, _RampMid, smoothstep(0.35 - _RampSoft, 0.35 + _RampSoft, ndl));
                t = lerp(t, 1.0, smoothstep(0.68 - _RampSoft, 0.68 + _RampSoft, ndl));

                float up = n.y * 0.5 + 0.5;
                float3 ambient = up < 0.5
                    ? lerp(unity_AmbientGround.rgb, unity_AmbientEquator.rgb, up * 2.0)
                    : lerp(unity_AmbientEquator.rgb, unity_AmbientSky.rgb, (up - 0.5) * 2.0);
                ambient *= _AmbientBoost;

                float shadow = SHADOW_ATTENUATION(i);
                float3 lit = ambient * lerp(0.55, 0.18, t) + _LightColor0.rgb * t * lerp(_ShadowTint.rgb, 1.0, shadow);

                float3 h = normalize(lightDir + i.view);
                float sheen = pow(saturate(dot(n, h)), _Shininess) * _Sheen * t * shadow;
                float rim = pow(1.0 - saturate(dot(n, i.view)), 3.0) * _Rim;

                fixed3 c = albedo.rgb * lit * lerp(1.0, i.col.r, 0.6) + _SheenColor.rgb * sheen + _RimColor.rgb * rim * t;
                UNITY_APPLY_FOG(i.fogCoord, c);
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
