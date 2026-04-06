Shader "Custom/2D/LineFlash"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Flash Settings)]
        [HDR] _FlashColor ("Flash Color", Color) = (1, 1, 1, 1)
        _FlashIntensity ("Flash Intensity", Range(0, 5)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            
            fixed4 _FlashColor;
            float _FlashIntensity;

            v2f vert(appdata_t IN) {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                // Cộng thêm màu Flash vào phần RGB hiện tại (chỉ cộng vào nơi có hình ảnh, tức Alpha > 0)
                c.rgb += _FlashColor.rgb * _FlashIntensity * c.a; 
                
                return c;
            }
            ENDCG
        }
    }
}