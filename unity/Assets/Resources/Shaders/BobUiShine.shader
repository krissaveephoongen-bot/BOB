// Battle of the Brooms: glTF-free uGUI gloss pass for the S28 card. A soft diagonal band sweeps across the sprite
// every _ShineSpeed seconds, tinted by _ShineTint, so panels and progress fills read as glossy paper instead of flat
// rectangles. Stencil properties are the same as UI/Default so the material keeps working inside a Mask.
Shader "BOB/UiShine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ShineTint ("Shine Tint", Color) = (1,1,1,0.5)
        _ShineWidth ("Band Width", Range(0.02, 0.6)) = 0.14
        _ShineSpeed ("Sweeps Per Second", Range(0.02, 2)) = 0.18
        _ShineStrength ("Strength", Range(0, 1)) = 0.32
        _ShineSkew ("Diagonal Skew", Range(0, 2)) = 0.5

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _ShineTint;
            float _ShineWidth, _ShineSpeed, _ShineStrength, _ShineSkew;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed4 col = tex * i.color;

                // Diagonal coordinate, normalised to 0..1 so the sweep fits any panel size.
                float span = 1.0 + _ShineSkew;
                float coord = (i.uv.x + i.uv.y * _ShineSkew) / span;
                float head = frac(_Time.y * _ShineSpeed);
                float band = 1.0 - smoothstep(0.0, _ShineWidth, abs(coord - head));
                band *= smoothstep(0.0, 0.08, head) * smoothstep(1.0, 0.92, head);

                col.rgb += _ShineTint.rgb * (band * _ShineStrength * _ShineTint.a) * tex.a;
                return col;
            }
            ENDCG
        }
    }

    Fallback "UI/Default"
}
