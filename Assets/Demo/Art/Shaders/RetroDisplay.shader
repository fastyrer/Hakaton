Shader "Hakaton/Retro Display"
{
    Properties { _MainTex("Frame", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            v2f vert(appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            half4 frag(v2f i) : SV_Target
            {
                float3 c = tex2D(_MainTex, i.uv).rgb;
                // Quantize perceptual colour, otherwise linear-space shadows turn into coloured dots.
                #ifndef UNITY_COLORSPACE_GAMMA
                c = LinearToGammaSpace(c);
                #endif
                int2 p = int2(i.uv * _MainTex_TexelSize.zw);
                // Ordered 4 x 4 dithering before 5-bit colour quantization.
                const float bayer[16] = { 0,8,2,10,12,4,14,6,3,11,1,9,15,7,13,5 };
                float d = (bayer[(p.y & 3) * 4 + (p.x & 3)] / 16.0 - 0.5) / 63.0;
                float2 edge = i.uv * (1 - i.uv);
                c *= lerp(0.72, 1.0, saturate(pow(edge.x * edge.y * 16, 0.32)));
                c = floor(saturate(c + d) * 31 + 0.5) / 31;
                #ifndef UNITY_COLORSPACE_GAMMA
                c = GammaToLinearSpace(c);
                #endif
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
