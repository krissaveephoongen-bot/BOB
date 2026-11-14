// Battle of the Brooms: a soft contact shadow for the Task Race map (docs 24). A quad with a radial falloff drawn
// straight onto the ground with multiplicative blending, so a counter, a bush or the hero reads as standing on the
// grass instead of pasted over it. Uses the same curve as BOB/CurvedWorld so the decal stays glued to the bent field.
Shader "BOB/GroundAO"
{
    Properties
    {
        _Color ("Shadow colour", Color) = (0.50,0.47,0.60,1)
        _Strength ("Strength", Range(0,1)) = 0.55
        _Softness ("Edge softness", Range(0.05,1)) = 0.55
        _CurvatureScale ("Curvature scale", Range(0,3)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" "IgnoreProjector"="True" "ForceNoShadowCasting"="True" }

        Pass
        {
            ZWrite Off
            ZTest LEqual
            Cull Off
            Blend DstColor Zero

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "BobCurvedWorld.hlsl"

            fixed4 _Color;
            float _Strength, _Softness, _CurvatureScale;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                float4 wp = mul(unity_ObjectToWorld, v.vertex);
                wp.xyz = BobCurvePosition(wp.xyz, _CurvatureScale);
                o.pos = mul(UNITY_MATRIX_VP, wp);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float r = saturate(length(i.uv - 0.5) * 2.0);
                float a = pow(1.0 - smoothstep(1.0 - _Softness, 1.0, r), 1.4) * _Strength;
                // Blend DstColor Zero multiplies the ground by this colour: white leaves it, the tint darkens it.
                return fixed4(lerp(fixed3(1,1,1), _Color.rgb, a), 1);
            }
            ENDCG
        }
    }
}
