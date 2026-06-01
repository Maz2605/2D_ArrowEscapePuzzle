Shader "UI/CircleCutout"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Center ("Center (Screen Pixels)", Vector) = (0,0,0,0)
        _Radius ("Radius (Pixels)", Float) = 0
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
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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
                float2 texcoord  : TEXCOORD0;
                float4 screenPosition : TEXCOORD1;
            };

            fixed4 _Color;
            float4 _Center;
            float _Radius;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                o.screenPosition = ComputeScreenPos(o.vertex);
                return o;
            }

            sampler2D _MainTex;

            fixed4 frag(v2f i) : SV_Target
            {
                // ComputeScreenPos provides coordinates in [0, w] range.
                // Divide by w to get normalized screen coordinates [0, 1].
                float2 screenUV = i.screenPosition.xy / i.screenPosition.w;
                
                // Convert to screen pixel space.
                float2 pixelPos = screenUV * _ScreenParams.xy;

                // Tính khoảng cách đến tâm vòng tròn đục lỗ
                float dist = distance(pixelPos, _Center.xy);

                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;

                // Tạo hiệu ứng chuyển tiếp (anti-aliased) mượt mà tại cạnh vòng tròn
                float alphaFactor = smoothstep(_Radius - 1.5, _Radius, dist);
                col.a *= alphaFactor;

                return col;
            }
            ENDCG
        }
    }
}
