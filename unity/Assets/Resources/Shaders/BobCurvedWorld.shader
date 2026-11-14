// Battle of the Brooms: BOB/CurvedWorld (Task Race map, docs 24). A 3-step toon ramp lit by the scene's trilight
// ambient and the main directional light, plus the Ever Planet mini-planet bend: every vertex drops by
// _BobCurvature * squared X-Z distance from the main camera, written by BobCurvedWorldController.cs as a global.
// Physics, colliders, NavMesh and the gameplay plane never move.
// Shadows are real: the ForwardBase pass samples the shadow map, and the ShadowCaster pass below bends its vertices the
// same way, so a cast shadow falls where the bent silhouette actually is instead of staying on the flat plane.
// An inverted-hull outline pass is built in so the map keeps the BOB line without a second material that would stay flat.
// Built-in render pipeline.
Shader "BOB/CurvedWorld"
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
        _Shininess ("Sheen tightness", Range(1,128)) = 24
        _Sheen ("Sheen strength", Range(0,0.5)) = 0.06
        _Mottle ("Ground mottle", Range(0,0.15)) = 0.035
        _RimColor ("Rim colour", Color) = (1,0.96,0.88,1)
        _Rim ("Rim strength", Range(0,1)) = 0.18
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
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "BobCurvedWorld.hlsl"

            fixed4 _OutlineColor; float _OutlinePixels, _CurvatureScale;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; UNITY_FOG_COORDS(1) };

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
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = _OutlineColor;
                UNITY_APPLY_FOG(i.fogCoord, c);
                return c;
            }
            ENDCG
        }

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
            #include "BobCurvedWorld.hlsl"

            sampler2D _MainTex; float4 _MainTex_ST;
            fixed4 _Color, _RimColor, _SheenColor, _ShadowTint, _DirtColor;
            float _RampLow, _RampMid, _RampSoft, _Rim, _DirtAmount, _CurvatureScale;
            float _AmbientBoost, _Shininess, _Sheen, _Mottle;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 nrm : TEXCOORD1;
                float3 view : TEXCOORD2;
                SHADOW_COORDS(3)
                float3 flatPos : TEXCOORD4;
                UNITY_FOG_COORDS(5)
                fixed4 col : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                float4 wp = mul(unity_ObjectToWorld, v.vertex);
                float4 flatWp = wp;
                wp.xyz = BobCurvePosition(wp.xyz, _CurvatureScale);
                o.pos = mul(UNITY_MATRIX_VP, wp);
                o.flatPos = flatWp.xyz;
                o.nrm = BobCurveNormal(UnityObjectToWorldNormal(v.normal), flatWp.xyz, _CurvatureScale);
                o.view = normalize(_WorldSpaceCameraPos - wp.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.col = v.color;
                TRANSFER_SHADOW_WPOS(o, wp);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 albedo = tex2D(_MainTex, i.uv) * _Color;
                float3 n = normalize(i.nrm);

                // A faint written-in mottle keeps the flat palette fields from reading as one printed green.
                float ground = saturate((n.y - 0.45) * 2.5);
                float mottle = sin(i.flatPos.x * 1.31 + sin(i.flatPos.z * 0.77)) * sin(i.flatPos.z * 1.07 + sin(i.flatPos.x * 0.61));
                albedo.rgb *= 1.0 + mottle * _Mottle * ground;
                albedo.rgb = lerp(albedo.rgb, albedo.rgb * 0.45 + _DirtColor.rgb * 0.55, _DirtAmount);

                float ndl = dot(n, normalize(_WorldSpaceLightPos0.xyz)) * 0.5 + 0.5;
                float t = _RampLow;
                t = lerp(t, _RampMid, smoothstep(0.35 - _RampSoft, 0.35 + _RampSoft, ndl));
                t = lerp(t, 1.0, smoothstep(0.68 - _RampSoft, 0.68 + _RampSoft, ndl));

                // The scene's trilight ambient (RenderSettings, AmbientMode.Trilight) instead of fixed material colours,
                // so one lighting setup drives the whole map.
                float up = n.y * 0.5 + 0.5;
                float3 ambient = up < 0.5
                    ? lerp(unity_AmbientGround.rgb, unity_AmbientEquator.rgb, up * 2.0)
                    : lerp(unity_AmbientEquator.rgb, unity_AmbientSky.rgb, (up - 0.5) * 2.0);
                ambient *= _AmbientBoost;

                // A shadow keeps a share of the key light (_ShadowTint) rather than going black: the pastel palette and
                // the painted-shadow look of the reference both need a readable, cool shadow.
                float shadow = SHADOW_ATTENUATION(i);
                float3 lit = ambient * lerp(0.55, 0.18, t) + _LightColor0.rgb * t * lerp(_ShadowTint.rgb, 1.0, shadow);

                float3 h = normalize(normalize(_WorldSpaceLightPos0.xyz) + i.view);
                float sheen = pow(saturate(dot(n, h)), _Shininess) * _Sheen * t * shadow;
                float rim = pow(1.0 - saturate(dot(n, i.view)), 3.0) * _Rim;

                fixed3 c = albedo.rgb * lit * lerp(1.0, i.col.r, 0.6);
                c += _SheenColor.rgb * sheen + _RimColor.rgb * rim * t;
                UNITY_APPLY_FOG(i.fogCoord, c);
                return fixed4(c, 1);
            }
            ENDCG
        }

        // The curved shadow caster: identical bias/inset maths to UnityClipSpaceShadowCasterPos, applied to the bent
        // world position so the shadow map sees the same dome the camera does.
        Pass
        {
            Name "SHADOWCASTER"
            Tags { "LightMode"="ShadowCaster" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
            #include "BobCurvedWorld.hlsl"

            float _CurvatureScale;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { V2F_SHADOW_CASTER; };

            v2f vert (appdata v)
            {
                v2f o;
                float4 wp = mul(unity_ObjectToWorld, v.vertex);
                wp.xyz = BobCurvePosition(wp.xyz, _CurvatureScale);
                if (unity_LightShadowBias.z != 0.0)
                {
                    float3 wN = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                    float cosA = dot(wN, normalize(UnityWorldSpaceLightDir(wp.xyz)));
                    wp.xyz -= wN * (unity_LightShadowBias.z * sqrt(saturate(1.0 - cosA * cosA)));
                }
                o.pos = UnityApplyLinearShadowBias(mul(UNITY_MATRIX_VP, wp));
                #if defined(SHADOWS_CUBE) && !defined(SHADOWS_CUBE_IN_DEPTH_TEX)
                o.vec = wp.xyz - _LightPositionRange.xyz;
                #endif
                return o;
            }

            float4 frag (v2f i) : SV_Target { SHADOW_CASTER_FRAGMENT(i) }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
