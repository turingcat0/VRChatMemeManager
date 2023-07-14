Shader "Unlit/MemeEmitterShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TextureU1("u1", Range(0.0,1.0)) = 0.0
        _TextureV1("v1", Range(0.0,1.0)) = 0.0
        _TextureU2("u2", Range(0.0,1.0)) = 0.0
        _TextureV2("v2", Range(0.0,1.0)) = 0.0
        _Frame("frame", Int) = 0
        _FrameWidth("frameWidth", Float) = 0.25
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
      

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            // float _MainTex_ST;
            float _TextureU1;
            float _TextureV1;
            float _TextureU2;
            float _TextureV2;
            int _Frame;
            float _FrameWidth;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float tmp = (_TextureU1 + _Frame* _FrameWidth);
                float tmp2 = floor(tmp);
                float realU1 = tmp - tmp2;
                float realU2 = realU1 + _FrameWidth;

                float realV1 =  (_TextureV1 - tmp2* _FrameWidth);
                float realV2 =  (_TextureV2 - tmp2* _FrameWidth);

                o.uv = v.uv * float2(realU2,realV2) + (float2(1, 1) - v.uv) * float2(realU1,realV1);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}
