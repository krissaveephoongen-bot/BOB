// Battle of the Brooms: BOB/CurvedWorld (Task Race map, docs 24). The same 3-step toon look as BOB/ToonLit plus the
// Ever Planet mini-planet bend: every vertex drops by _BobCurvature * squared X-Z distance from the main camera, written
// by BobCurvedWorldController.cs as a global. Physics, colliders, NavMesh and the gameplay plane never move.
// An inverted-hull outline pass is built in so the map keeps the BOB line without a second material that would stay flat.
// Built-in render pipeline. Shadows use the FallBack pass, so they are cast from the flat mesh (acceptable for the map).
Shader "BOB/CurvedWorld"
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
        _CurvatureScale ("Curvature scale", Range(0,3)) = 1
        _OutlineColor ("Outline colour", Color) = (0.29,0.18,0.133,1)
        _OutlinePixels ("Outline width (screen px)", Range(0,8)) = 1.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        // Inverted hull, drawn first: pushed out in clip space so the line keeps a constant screen width. It uses the
        // same curved vertex so the outline hugs the bent silhouette instead of floating on the flat plane.
        Pass
        {
            Name "OUTLINE"
            Cull Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "BobCurvedWorld.hlsl"

            fixed4 _OutlineColor; float _OutlinePixels, _CurvatureScale;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; };

            v2f vert (appdata v)
            {
                v2f o;
                float4 wp = mul(unity_ObjectToWorld, v.vertex);
                wp.xyz = BobCurvePosition(wp.xyz, _CurvatureScale);
                float4 clip = mul(UNITY_MATRIX_VP, wp);
                float3 n = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
                float2 dir = normalize(mul((float2x2)UNITY_MATRIX_P, n.xy));
                float2 px = _OutlinePixels * 2.0 / _ScreenParams.xy;
                clip.xy += dir * px * clip.w;
                o.pos = clip;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target { return _OutlineColor; }
            ENDCG
        }

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "BobCurvedWorld.hlsl"

            sampler2D _MainTex; float4 _MainTex_ST;
            fixed4 _Color, _RimColor, _SkyColor, _GroundColor, _DirtColor;
            float _RampLow, _RampMid, _RampSoft, _Rim, _DirtAmount, _CurvatureScale;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 nrm : TEXCOORD1; float3 view : TEXCOORD2; fixed4 col : COLOR; };

            v2f vert (appdata v)
            {
                v2f o;
                float4 wp = mul(unity_ObjectToWorld, v.vertex);
                float4 flatWp = wp;
                wp.xyz = BobCurvePosition(wp.xyz, _CurvatureScale);
                o.pos = mul(UNITY_MATRIX_VP, wp);
                o.nrm = BobCurveNormal(UnityObjectToWorldNormal(v.normal), flatWp.xyz, _CurvatureScale);
                o.view = normalize(_WorldSpaceCameraPos - wp.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
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
