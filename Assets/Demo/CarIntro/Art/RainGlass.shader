Shader "Hakaton/Car Rain Glass"
{
    Properties{_IntroTime("Film clock",Float)=0 _Tint("Tint",Color)=(.10,.16,.19,1)}
    SubShader
    {
        Tags{"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
            float _IntroTime;float4 _Tint;
            V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;return o;}
            half4 frag(V i):SV_Target
            {
                float column=floor(i.uv.x*43);float seed=frac(sin(column*17.31)*43758.5453);
                float x=abs(frac(i.uv.x*43)-.5);float y=frac(i.uv.y*1.7+_IntroTime*(.08+seed*.1)+seed);
                float drop=smoothstep(.16,.02,x)*smoothstep(.17,.005,abs(y-.5));
                float trail=smoothstep(.07,.01,x)*smoothstep(.48,.03,abs(y-.40));
                return half4(_Tint.rgb+drop*.28,.025+drop*.32+trail*.10);
            }
            ENDHLSL
        }
    }
}
