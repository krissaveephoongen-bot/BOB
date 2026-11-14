// Battle of the Brooms: the pastel sea of cloud under the Task Race islands (docs 24). The map floats over the sky;
// without something underneath, the cliffs end in nothing and the whole piece reads as a cut-out. This draws a soft
// cloud sheet a few units below the play plane, bent by the same curve as the map and faded at its rim, so the islands
// sit on a layer of weather instead of on empty blue.
Shader "BOB/CloudSea"
{
    Properties
    {
        _MainTex ("Clouds", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Opacity ("Opacity", Range(0,1)) = 0.9
        _CurvatureScale ("Curvature scale", Range(0,3)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent-50" "RenderType"="Transparent" "IgnoreProjector"="True" "ForceNoShadowCasting"="True" }

        Pass
        {
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "BobCurvedWorld.hlsl"

            sampler2D _MainTex;
            fixed4 _Color;
            float _Opacity, _CurvatureScale;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; UNITY_FOG_COORDS(1) };

            v2f vert (appdata v)
            {
                v2f o;
                float4 wp = mul(unity_ObjectToWorld, v.vertex);
                wp.xyz = BobCurvePosition(wp.xyz, _CurvatureScale);
                o.pos = mul(UNITY_MATRIX_VP, wp);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * _Color;
                c.a *= _Opacity;
                UNITY_APPLY_FOG(i.fogCoord, c);
                return c;
            }
            ENDCG
        }
    }
}
