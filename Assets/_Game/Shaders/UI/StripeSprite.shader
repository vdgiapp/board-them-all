Shader "UI/StripeSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StripeColorA ("Stripe Color A", Color) = (1,1,1,1)
        _StripeColorB ("Stripe Color B", Color) = (0,0,0,1)
        _StripeWidth ("Stripe Width (pixels)", Range(1, 200)) = 20
        _StripeAngle ("Stripe Angle (deg)", Range(0, 360)) = 45
        _StripeSpeed ("Scroll Speed", Float) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
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
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 mask : TEXCOORD2;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _MainTex_ST;
            float4 _ClipRect;

            fixed4 _StripeColorA;
            fixed4 _StripeColorB;
            float _StripeWidth;
            float _StripeAngle;
            float _StripeSpeed;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd;
                fixed4 spriteCol = texColor * IN.color;

                // Rotate UV for angled stripes
                float rad = radians(_StripeAngle);
                float s = sin(rad);
                float c = cos(rad);
                //float2 uv = IN.texcoord - 0.5;
                float2 uv = IN.worldPosition.xy;
                float2 rotatedUV = float2(uv.x * c - uv.y * s, uv.x * s + uv.y * c);

                float stripePos = rotatedUV.x + _StripeSpeed * _Time.y;
                float stripe = frac(stripePos / _StripeWidth);
                fixed4 stripeCol = lerp(_StripeColorB, _StripeColorA, step(0.5, stripe));

                // fixed4 col = stripeCol;
                // col.a *= spriteCol.a;

                fixed4 col;
                col.rgb = spriteCol.rgb * stripeCol.rgb;   // nhân màu sọc với màu sprite gốc
                col.a = spriteCol.a * stripeCol.a;         // giữ alpha gốc của sprite
                
                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}