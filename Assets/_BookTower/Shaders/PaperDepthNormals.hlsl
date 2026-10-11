#ifndef BOOK_TOWER_PAPER_DEPTH_NORMALS_INCLUDED
#define BOOK_TOWER_PAPER_DEPTH_NORMALS_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

struct PaperDepthAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
};
struct PaperDepthVaryings
{
    float4 positionCS : SV_POSITION;
    float3 normalWS : TEXCOORD0;
};
PaperDepthVaryings PaperDepthVertex(PaperDepthAttributes input)
{
    PaperDepthVaryings output;
    // Match the forward pass exactly so depth priming's Equal test retains the ink.
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    return output;
}
half4 PaperDepthFragment(PaperDepthVaryings input, FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC) : SV_Target
{
    float3 normalWS = normalize(input.normalWS);
    #if defined(PAPER_FLIP_BACKFACE_NORMALS)
        normalWS *= IS_FRONT_VFACE(facing, 1, -1);
    #endif
    #if defined(_GBUFFER_NORMALS_OCT)
        float2 octNormal = PackNormalOctQuadEncode(normalWS);
        return half4(PackFloat2To888(saturate(octNormal * .5 + .5)), 0);
    #else
        return half4(normalWS, 0);
    #endif
}
#endif
