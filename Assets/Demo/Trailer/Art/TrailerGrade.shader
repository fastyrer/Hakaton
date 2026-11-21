Shader "Hakaton/Trailer Grade"
{
    Properties { _MainTex("Frame",2D)="white"{} _Mono("Memory",Float)=0 _TrailerTime("Time",Float)=0 _Darken("Title shade",Float)=0 }
    SubShader
    {
        Tags { "Queue"="Transparent" }
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct v2f{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};
            sampler2D _MainTex;float _TrailerTime,_Mono,_Darken;
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
            half4 frag(v2f i):SV_Target
            {
                float3 c=tex2D(_MainTex,i.uv).rgb;
                #ifndef UNITY_COLORSPACE_GAMMA
                c=LinearToGammaSpace(c);
                #endif
                c*=1.13;float l=dot(c,float3(.2126,.7152,.0722));
                c=lerp(l.xxx,c,.57);c=(c-.42)*1.12+.42;
                c+=float3(-.025,.018,.022)*(1-l)+float3(.055,.025,-.025)*l;
                c=lerp(c,l.xxx*float3(1.03,1,.93),_Mono*.93);
                float2 d=i.uv-.5;float vig=saturate(1-dot(d,d)*.88);
                c*=vig*(1-_Darken*.78);
                float noise=frac(sin(dot(floor(i.uv*float2(640,360)),float2(12.9898,78.233))+floor(_TrailerTime*24))*43758.5453)-.5;
                c=floor(saturate(c+noise/120)*63+.5)/63;
                #ifndef UNITY_COLORSPACE_GAMMA
                c=GammaToLinearSpace(c);
                #endif
                return half4(c,1);
            }
            ENDHLSL
        }
    }
}
