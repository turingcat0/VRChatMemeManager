Shader "MemeManager/MemeEmitterShader"
{
    Properties
    {
         _MainTex ("Tex", 2D) = "" {}
        _AspectRatio("aspect", float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" "IgnoreProjector"="True"}
        Blend SrcAlpha OneMinusSrcAlpha
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
            float _AspectRatio;
            int _Timer;

            v2f vert (appdata v)
            {
                v2f o;
                float4 clipPos = UnityObjectToClipPos(v.vertex);
                clipPos.y *= _AspectRatio;
                o.vertex = clipPos;
                o.uv.xy = v.uv.xy;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                return  tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
