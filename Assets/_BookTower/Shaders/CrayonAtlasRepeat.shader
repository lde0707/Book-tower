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
   #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
   #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
   #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
   struct V{float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD1;float2 uv:TEXCOORD0;float3 positionWS:TEXCOORD2;};
   V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.positionWS=TransformObjectToWorld(i.positionOS.xyz);o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.uv=i.uv;return o;}
   half4 frag(V i, FRONT_FACE_TYPE facing:FRONT_FACE_SEMANTIC):SV_Target
   {
    float2 f=frac(i.uv);float2 uv=f*.46+float2(.52,.02);
    half3 paper=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv).rgb;
    float edge=min(min(f.x,1-f.x),min(f.y,1-f.y));
    paper=lerp(half3(.91,.84,.70),paper,smoothstep(0,.035,edge));
    half3 n=normalize(i.normalWS)*IS_FRONT_VFACE(facing,1,-1);
    half3 illumination=max(SampleSH(n),half3(.018,.018,.02));
    Light sun=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
    illumination+=sun.color*saturate(dot(n,sun.direction))*sun.distanceAttenuation*sun.shadowAttenuation;
    #if defined(_ADDITIONAL_LIGHTS) || USE_CLUSTER_LIGHT_LOOP
    InputData inputData=(InputData)0;inputData.positionWS=i.positionWS;inputData.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
    uint count=GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(count)
     Light l=GetAdditionalLight(lightIndex,i.positionWS,half4(1,1,1,1));
     illumination+=l.color*(.08+.92*saturate(dot(n,l.direction)))*l.distanceAttenuation*l.shadowAttenuation;
    LIGHT_LOOP_END
    #endif
    half ao=GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(i.positionCS)).indirectAmbientOcclusion;
    return half4(paper*half3(.94,.90,.81)*illumination*ao,1);
   }
   ENDHLSL
  }
  Pass
  {
   Name "DepthOnly" Tags {"LightMode"="DepthOnly"} ZWrite On ColorMask R
   HLSLPROGRAM
   #pragma vertex vertDepth
   #pragma fragment fragDepth
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   float4 vertDepth(float4 p:POSITION):SV_POSITION{return TransformObjectToHClip(p.xyz);}
   half4 fragDepth():SV_Target{return 0;}
   ENDHLSL
  }
  Pass
  {
   Name "DepthNormals" Tags {"LightMode"="DepthNormalsOnly"} ZWrite On
   HLSLPROGRAM
   #pragma vertex PaperDepthVertex
   #pragma fragment PaperDepthFragment
   #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
   #define PAPER_FLIP_BACKFACE_NORMALS
   #include "PaperDepthNormals.hlsl"
   ENDHLSL
  }
  UsePass "BookTower/ReadingPaper/ShadowCaster"
 }
}
