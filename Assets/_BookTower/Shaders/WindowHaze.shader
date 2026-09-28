Shader "BookTower/WindowHaze"
{
 Properties{_Color("Warm scattering",Color)=(1,.84,.57,.075) _Particle("Particle fade",Float)=0}
 SubShader
 {
  Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Pass
  {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
   struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
   CBUFFER_START(UnityPerMaterial)
   half4 _Color;
   float _Particle;
   CBUFFER_END
   V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;o.color=lerp(half4(1,1,1,1),i.color,_Particle);return o;}
   half4 frag(V i):SV_Target
   {
    float edge=pow(saturate(1-abs(i.uv.x*2-1)),1.8);
    float ends=smoothstep(0,.12,i.uv.y)*(1-smoothstep(.55,1,i.uv.y));
    float shape=lerp(edge*ends,pow(saturate(1-length(i.uv*2-1)),2),_Particle);
    return half4(_Color.rgb*i.color.rgb,_Color.a*shape*i.color.a);
   }
   ENDHLSL
  }
 }
}
