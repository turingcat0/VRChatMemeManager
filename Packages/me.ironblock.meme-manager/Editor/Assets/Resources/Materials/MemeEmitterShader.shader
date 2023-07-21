Shader "Unlit/MemeEmitterShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TextureU1("u1", Range(0.0,1.0)) = 0.0
        _TextureV1("v1", Range(0.0,1.0)) = 0.0
        _TextureU2("u2", Range(0.0,1.0)) = 0.0
        _TextureV2("v2", Range(0.0,1.0)) = 0.0
        _AspectRatio("aspect", float) = 1.0
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
            float _AspectRatio;

            v2f vert (appdata v)
            {
                v2f o;
                float4 clipPos = UnityObjectToClipPos(v.vertex);
                clipPos.y *= _AspectRatio;
                o.vertex = clipPos;

                o.uv = float2(_TextureU2 * v.uv.x + _TextureU1 * (1 - v.uv.x), _TextureV2 * v.uv.y + _TextureV1 * (1 - v.uv.y));
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
