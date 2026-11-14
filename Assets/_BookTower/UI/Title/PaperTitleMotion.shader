Shader "BookTower/UI/TitlePaperMotion"
{
    Properties
    {
        [PerRendererData] _MainTex ("Paper artwork", 2D) = "white" {}
        _TitleTime ("Unscaled title time", Float) = 0
        _Motion ("Motion", Float) = 1
        _Light ("Light variation", Float) = 1
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 worldPosition : TEXCOORD1; };
            sampler2D _MainTex;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _TitleTime, _Motion, _Light;
            v2f vert(appdata v)
            {
                v2f o; o.worldPosition=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o;
            }
            float falloff(float2 uv, float2 center, float2 radius)
            { float2 p=(uv-center)/radius; return exp(-dot(p,p)*2); }
            fixed4 frag(v2f i) : SV_Target
            {
                float t=_TitleTime;
                float world=smoothstep(.29,.44,i.uv.x);
                float2 uv=i.uv;
                uv += world*_Motion*float2(sin(t*.24)*.00075, sin(t*.31)*.0007);
                uv.y += _Motion*falloff(i.uv,float2(.759,.505),float2(.074,.12))*sin(t*.8)*.0019;
                fixed4 c=(tex2D(_MainTex,uv)+_TextureSampleAdd)*i.color;
                float glow=falloff(i.uv,float2(.679,.310),float2(.022,.04));
                glow += .6*falloff(i.uv,float2(.695,.452),float2(.016,.025));
                c.rgb += _Light*(.018+.012*sin(t*1.15))*glow*float3(1,.65,.19);
                #ifdef UNITY_UI_CLIP_RECT
                c.a *= UnityGet2DClipping(i.worldPosition.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(c.a-.001);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
