Shader "BookTower/F0PaperLight" {
Properties { _Color("Color",Color)=(1,1,1,1) }
SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
Pass { Blend SrcAlpha One ZWrite Off Cull Off ZTest LEqual
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
CBUFFER_START(UnityPerMaterial)
float4 _Color;
CBUFFER_END
V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;return o;}
half4 frag(V i):SV_Target {float soft=pow(saturate(1-abs(i.uv.y*2-1)),2);return half4(_Color.rgb,_Color.a*soft);}
ENDHLSL
}}}
