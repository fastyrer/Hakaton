Shader "Hakaton/Menu Grade"
{
    Properties { _MainTex("World",2D)="white"{} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct a{float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct v{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};
            sampler2D _MainTex;
            v vert(a i){v o;o.vertex=UnityObjectToClipPos(i.vertex);o.uv=i.uv;return o;}
            half4 frag(v i):SV_Target
            {
                float3 c=tex2D(_MainTex,i.uv).rgb;
                #ifndef UNITY_COLORSPACE_GAMMA
                c=LinearToGammaSpace(c);
                #endif
                float l=dot(c,float3(.2126,.7152,.0722));c=lerp(l.xxx,c,.8);
                c=(c-.4)*1.06+.4;c*=float3(1.07,1.01,.91);
                float2 d=i.uv-.5;c*=1-dot(d,d)*.42;
                float n=frac(sin(dot(floor(i.uv*float2(1280,720)),float2(12.9898,78.233)))*43758.5453)-.5;
                c=floor(saturate(c+n/180)*63+.5)/63;
                #ifndef UNITY_COLORSPACE_GAMMA
                c=GammaToLinearSpace(c);
                #endif
                return half4(c,1);
            }
            ENDHLSL
        }
    }
}
