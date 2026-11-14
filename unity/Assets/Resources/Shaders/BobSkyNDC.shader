// Battle of the Brooms: the written sky, drawn in clip space (docs 24 section 3.1). The mesh vertices are already
// normalised device coordinates, so the quad covers the whole frame whatever the camera FOV or aspect is; a sky quad
// placed at a fixed world depth showed its own edge in the wide shot whenever the aspect changed.
// Drawn in the Background queue with ZWrite off and ZTest always: it paints first and every model overdraws it.
Shader "BOB/SkyNDC"
{
    Properties
    {
        _MainTex ("Sky", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "IgnoreProjector"="True" "PreviewType"="Skybox" }

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = float4(v.vertex.xy, 0.0, 1.0);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
