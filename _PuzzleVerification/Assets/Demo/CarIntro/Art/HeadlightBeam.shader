Shader "Hakaton/Car Headlight Mist"
{
    Properties{_Tint("Tint",Color)=(.68,.75,.63,.035)}
    SubShader
    {
        Tags{"Queue"="Transparent-10" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A{float4 p:POSITION;float4 c:COLOR;};struct V{float4 p:SV_POSITION;float4 c:COLOR;};float4 _Tint;
            V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.c=a.c;return o;}
            half4 frag(V i):SV_Target{return half4(_Tint.rgb,_Tint.a*i.c.a);}
            ENDHLSL
        }
    }
}
