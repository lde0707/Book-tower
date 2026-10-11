Shader "BookTower/InteriorAtmosphere"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Bounded paper air"
            ZTest Always ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            int _PaperAirCount, _PaperBeamCount, _PaperAirSteps;
            float4 _PaperAirMin[3], _PaperAirMax[3], _PaperAirColor[3], _PaperAirSettings[3];
            float4 _PaperBeamPosition[6], _PaperBeamDirection[6], _PaperBeamColor[6], _PaperBeamRight[6], _PaperBeamUp[6];
            TEXTURE2D(_PaperBeamDepth0); TEXTURE2D(_PaperBeamDepth1); TEXTURE2D(_PaperBeamDepth2);
            TEXTURE2D(_PaperBeamDepth3); TEXTURE2D(_PaperBeamDepth4); TEXTURE2D(_PaperBeamDepth5);
            float DepthAt(int index, float2 uv)
            {
                // A single initialized return avoids FXC's uninitialized-return warning
                // when this function is inlined into the nested raymarch loops.
                float result=1.0;
                if(index==0) result=SAMPLE_TEXTURE2D_LOD(_PaperBeamDepth0,sampler_LinearClamp,uv,0).r;
                else if(index==1) result=SAMPLE_TEXTURE2D_LOD(_PaperBeamDepth1,sampler_LinearClamp,uv,0).r;
                else if(index==2) result=SAMPLE_TEXTURE2D_LOD(_PaperBeamDepth2,sampler_LinearClamp,uv,0).r;
                else if(index==3) result=SAMPLE_TEXTURE2D_LOD(_PaperBeamDepth3,sampler_LinearClamp,uv,0).r;
                else if(index==4) result=SAMPLE_TEXTURE2D_LOD(_PaperBeamDepth4,sampler_LinearClamp,uv,0).r;
                else if(index==5) result=SAMPLE_TEXTURE2D_LOD(_PaperBeamDepth5,sampler_LinearClamp,uv,0).r;
                return result;
            }
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv=input.texcoord;
                half4 scene=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv);
                if(_PaperAirCount==0) return scene;
                float depth=SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
                #endif
                float3 end=ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP);
                float3 origin=GetCameraPositionWS();
                float lengthToSurface=min(length(end-origin),140);
                float3 ray=normalize(end-origin);
                float3 invRay=rcp(ray+float3(1e-6,1e-6,1e-6));
                float3 scatter=0; float transmission=1;
                // Integrate only the room segment between the camera and the first visible surface.
                for(int room=0;room<_PaperAirCount;room++)
                {
                    float3 aa=(_PaperAirMin[room].xyz-origin)*invRay;
                    float3 bb=(_PaperAirMax[room].xyz-origin)*invRay;
                    float3 lo=min(aa,bb),hi=max(aa,bb);
                    float enter=max(max(lo.x,max(lo.y,lo.z)),0);
                    float leave=min(min(hi.x,min(hi.y,hi.z)),lengthToSurface);
                    // Near surfaces have exactly zero density and shaft contribution.
                    // Keep the same integration grid for all remaining samples.
                    if(leave<=enter || leave<=_PaperAirSettings[room].y) continue;
                    int steps=max(8,_PaperAirSteps);
                    float dt=(leave-enter)/steps;
                    float jitter=frac(52.9829189*frac(dot(input.positionCS.xy,float2(.06711056,.00583715))));
                    for(int s=0;s<steps;s++)
                    {
                        float dist=enter+(s+.2+.6*jitter)*dt;
                        float3 p=origin+ray*dist;
                        float clear=smoothstep(_PaperAirSettings[room].y,_PaperAirSettings[room].y+8,dist);
                        if(clear<=0) continue;
                        float height=saturate((p.y-_PaperAirSettings[room].z)/12);
                        float density=_PaperAirSettings[room].x*clear*lerp(.75,1.25,height);
                        float alpha=1-exp(-density*dt);
                        float3 incoming=_PaperAirColor[room].rgb;
                        // Static environment shafts have their own baked architectural occlusion.
                        // Dynamic shadow-puzzle projection is deliberately not included in this effect.
                        for(int b=0;b<_PaperBeamCount;b++)
                        {
                            float3 delta=p-_PaperBeamPosition[b].xyz;
                            float axial=dot(delta,_PaperBeamDirection[b].xyz);
                            if(axial<.3||axial>_PaperBeamPosition[b].w) continue;
                            float radius=max(.1,axial*_PaperBeamDirection[b].w);
                            float2 buv=float2(dot(delta,_PaperBeamRight[b].xyz),dot(delta,_PaperBeamUp[b].xyz))/radius;
                            float edge=1-smoothstep(.28,1,dot(buv,buv));
                            if(edge<=0) continue;
                            float blocker=DepthAt(b,buv*.5+.5)*_PaperBeamPosition[b].w;
                            float visibility=1-smoothstep(blocker-.25,blocker+.15,length(delta));
                            float falloff=pow(saturate(1-axial/_PaperBeamPosition[b].w),1.4);
                            float facing=saturate(dot(-ray,_PaperBeamDirection[b].xyz));
                            float facing2=facing*facing;
                            float phase=.6+.4*facing2*facing2;
                            float beam=_PaperBeamColor[b].a*edge*falloff*visibility*phase*clear;
                            scatter+=transmission*_PaperBeamColor[b].rgb*beam*dt;
                        }
                        scatter+=transmission*incoming*alpha;
                        transmission*=1-alpha;
                    }
                }
                return half4(scene.rgb*transmission+scatter,scene.a);
            }
            ENDHLSL
        }
    }
}
