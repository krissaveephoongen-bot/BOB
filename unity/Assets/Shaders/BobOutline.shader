// Battle of the Brooms: inverted-hull outline (Art Bible 21 section 5.1, M_BOB_Outline). Draw a copy of the mesh pushed out
// along its normals in clip space so the line is a constant number of pixels wide on screen. Colour #4A2E22 by default.
Shader "BOB/Outline"
{
    Properties
    {
        _OutlineColor ("Outline colour", Color) = (0.29,0.18,0.133,1)
        _Pixels ("Width (screen px)", Range(0,8)) = 2.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
        Pass
        {
            Cull Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _OutlineColor; float _Pixels;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; };
            v2f vert (appdata v)
            {
                v2f o;
                float4 clip = UnityObjectToClipPos(v.vertex);
                float3 n = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
                float2 dir = normalize(mul((float2x2)UNITY_MATRIX_P, n.xy));
                float2 px = _Pixels * 2.0 / _ScreenParams.xy;
                clip.xy += dir * px * clip.w;
                o.pos = clip;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target { return _OutlineColor; }
            ENDCG
        }
    }
}
