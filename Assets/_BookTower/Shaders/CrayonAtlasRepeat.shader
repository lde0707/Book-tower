Shader "BookTower/CrayonAtlasRepeat"
{
 Properties { _BaseMap("Crayon atlas",2D)="white"{} }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
  Cull Off
  Pass
  {
   Tags { "LightMode"="UniversalForward" }
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
   struct V{float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD1;float2 uv:TEXCOORD0;};
   V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.uv=i.uv;return o;}
   half4 frag(V i):SV_Target{float2 f=frac(i.uv);float2 uv=f*.46+float2(.52,.02);half3 paper=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv).rgb;float edge=min(min(f.x,1-f.x),min(f.y,1-f.y));paper=lerp(half3(.91,.84,.70),paper,smoothstep(0,.035,edge));Light sun=GetMainLight();half amount=.62+.20*abs(dot(normalize(i.normalWS),sun.direction));return half4(paper*half3(.87,.75,.56)*amount,1);}
   ENDHLSL
  }
 }
}
