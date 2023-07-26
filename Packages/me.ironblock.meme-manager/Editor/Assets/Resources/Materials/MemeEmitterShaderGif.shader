Shader "MemeManager/MemeEmitterShader"
{
    Properties
    {
         _MainTex ("Tex", 2DArray) = "" {}
        _AspectRatio("aspect", float) = 1.0
        _Timer("Timer", Int) = 0
        _FPS("FPS", Int) = 0
        _Length("Length", Int) = 0      //表示动图有多少帧
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma require 2darray

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float3 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            UNITY_DECLARE_TEX2DARRAY (_MainTex);
            float _AspectRatio;
            int _Timer;
            int _FPS;
            int _Length;

            v2f vert (appdata v)
            {
                v2f o;
                float4 clipPos = UnityObjectToClipPos(v.vertex);
                clipPos.y *= _AspectRatio;
                o.vertex = clipPos;
                o.uv.xy = v.uv.xy;
                o.uv.z = ((uint)(_Timer * (_FPS / 60.0f)) % _Length);
                // o.uv.z = 0;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                return UNITY_SAMPLE_TEX2DARRAY (_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
