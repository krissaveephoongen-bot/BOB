// Battle of the Brooms: the detail shader for seeded map dressing — grass blades, daisies, stones, the footbridge and
// the waterfall (docs 24 section 3.1). Same curve/lighting/strata maths as BOB/CurvedWorld, minus the inverted-hull
// outline pass: that pass draws the *back* of every thin blade in the dark outline colour at zero screen width, which
// turned the grass into brown thorns. Two-sided instead, so a single triangle works from either side of the camera.
Shader "BOB/Dressing"
{
    Properties
    {
        _MainTex ("Palette", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ShadowTint ("Shadow tint", Color) = (0.55,0.58,0.78,1)
        _RimColor ("Rim colour", Color) = (1,0.96,0.88,1)
        _Rim ("Rim", Range(0,1)) = 0
        _SheenColor ("Sheen colour", Color) = (1,1,1,1)
        _Sheen ("Sheen", Range(0,1)) = 0.06
        _Shininess ("Shininess", Range(1,64)) = 24
        _RampLow ("Ramp low", Range(0,1)) = 0.45
        _RampMid ("Ramp mid", Range(0,1)) = 0.78
        _RampSoft ("Ramp soft", Range(0,0.5)) = 0.08
        _AmbientBoost ("Ambient boost", Range(0,2)) = 1
        _Mottle ("Ground mottle", Range(0,0.5)) = 0
        _Strata ("Rock strata", Range(0,0.5)) = 0
        _StrataScale ("Strata scale", Range(0,16)) = 2.2
        _CliffShade ("Underside shade", Color) = (0.55,0.48,0.58,1)
        _DirtAmount ("Dirt", Range(0,1)) = 0
        _DirtColor ("Dirt colour", Color) = (0.42,0.34,0.36,1)
        _CurvatureScale ("Curvature scale", Range(0,3)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off

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
            float _Strata, _StrataScale;
            fixed4 _CliffShade;

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

                float ground = saturate((n.y - 0.45) * 2.5);
                float wall = saturate((0.45 - n.y) * 2.0);
                float m1 = sin(i.flatPos.x * 1.31 + sin(i.flatPos.z * 0.77)) * sin(i.flatPos.z * 1.07 + sin(i.flatPos.x * 0.61));
                float m2 = sin(i.flatPos.x * 3.7 + 1.7) * sin(i.flatPos.z * 2.9 + 0.6);
                albedo.rgb *= 1.0 + (m1 + m2 * 0.55) * _Mottle * ground;

                float strata = sin(i.flatPos.y * _StrataScale + m1 * 1.4) * 0.5 + 0.5;
                float3 rock = albedo.rgb * lerp(1.0 - _Strata, 1.0 + _Strata * 0.35, strata);
                float depthAO = saturate((i.flatPos.y + 8.0) * 0.125);
                rock *= lerp(_CliffShade.rgb, float3(1, 1, 1), depthAO);
                albedo.rgb = lerp(albedo.rgb, rock, wall);
                albedo.rgb = lerp(albedo.rgb, albedo.rgb * 0.45 + _DirtColor.rgb * 0.55, _DirtAmount);

                float ndl = dot(n, normalize(_WorldSpaceLightPos0.xyz)) * 0.5 + 0.5;
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
