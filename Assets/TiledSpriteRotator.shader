Shader "Custom/TiledSpriteRotator"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        
        // 用于控制是否需要旋转 UV (1为是，0为否)
        _IsVertical ("Is Vertical", Float) = 0
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

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha // 预乘 Alpha 混合

        Pass
        {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"

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
            };

            fixed4 _Color;
            float _IsVertical;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap (OUT.vertex);
                #endif

                return OUT;
            }

            sampler2D _MainTex;

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                
                // 顺时针旋转90度：x' = y, y' = 1 - x
                float2 rotatedUV = float2(uv.y, 1.0 - uv.x);
                
                // 使用 lerp 替代 if 分支判定，对 GPU 性能更友好
                // 当 _IsVertical 为 0 时，保持原 uv；为 1 时，使用 rotatedUV
                uv = lerp(uv, rotatedUV, _IsVertical);

                // 采样并乘以顶点颜色
                fixed4 c = tex2D(_MainTex, uv) * IN.color;
                
                // Unity Sprite 默认使用预乘 Alpha
                c.rgb *= c.a; 
                return c;
            }
        ENDCG
        }
    }
}