Shader "ConvergenceHall/Lounge Matte Surface"
{
    Properties
    {
        [MainTexture] _BaseMap("Fine surface grain",2D)="white"{}
        [MainColor] _BaseColor("Surface colour",Color)=(1,1,1,1)
        _TextureMetres("Grain repeat metres",Float)=0.35
        [HideInInspector] _Surface("Surface",Float)=0
        [HideInInspector] _SrcBlend("Source blend",Float)=1
        [HideInInspector] _DstBlend("Destination blend",Float)=0
        [HideInInspector] _ZWrite("Depth write",Float)=1
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        Pass
        {
            Name "MatteSurface"
            Tags {"LightMode"="UniversalForward"}
            Cull Back ZWrite [_ZWrite] Blend [_SrcBlend] [_DstBlend]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _TextureMetres,_Surface,_SrcBlend,_DstBlend,_ZWrite;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
            struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;};
            Varyings Vert(Attributes input)
            {
                Varyings o;o.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                o.positionWS=TransformObjectToWorld(input.positionOS.xyz);o.normalWS=TransformObjectToWorldNormal(input.normalOS);return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                half3 n=normalize(i.normalWS);float3 weights=pow(abs(n),4);weights/=max(.001,dot(weights,float3(1,1,1)));
                half3 grain=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.positionWS.yz/_TextureMetres).rgb*weights.x
                    +SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.positionWS.xz/_TextureMetres).rgb*weights.y
                    +SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.positionWS.xy/_TextureMetres).rgb*weights.z;
                // Diffuse room bounce plus directional shading; no emission or screen-space glow.
                Light main=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half shade=.65+.27*saturate(dot(n,main.direction))*lerp(.35,1,main.shadowAttenuation)+.08*saturate(n.y);
                return half4(_BaseColor.rgb*grain*shade,_BaseColor.a);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
