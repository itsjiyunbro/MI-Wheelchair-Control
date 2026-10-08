Shader "ConvergenceHall/B112 Exposed Concrete"
{
    Properties
    {
        _BaseMap("Concrete grain",2D)="white"{}
        _BaseColor("Concrete tint",Color)=(0.82,0.83,0.80,1)
        _TextureMetres("Texture repeat in metres",Float)=1.6
        _PanelWidth("Panel width",Float)=1.6
        _PanelHeight("Panel height",Float)=1.15
        _JointWidth("Joint width",Float)=0.003
        [HideInInspector] _Surface("Surface",Float)=0
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        Pass
        {
            Name "ConcreteSurface"
            Tags {"LightMode"="UniversalForward"}
            Cull Back ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _TextureMetres,_PanelWidth,_PanelHeight,_JointWidth,_Surface;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
            struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;};
            Varyings Vert(Attributes input)
            {
                Varyings output;output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS=TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                half3 normal=normalize(input.normalWS);float2 metres;
                if(abs(normal.y)>.7)metres=input.positionWS.xz;
                else if(abs(normal.x)>.7)metres=float2(input.positionWS.z,input.positionWS.y);
                else metres=input.positionWS.xy;
                half3 grain=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,metres/_TextureMetres).rgb;
                float2 panel=frac((metres+float2(.32,.29))/float2(_PanelWidth,_PanelHeight));
                float2 distance=min(panel,1-panel)*float2(_PanelWidth,_PanelHeight);
                float edge=min(distance.x,distance.y);
                half joint=lerp(.86,1,smoothstep(_JointWidth*.35,_JointWidth*1.35,edge));
                // A small shading range preserves depth without changing the apparent wall finish per face.
                Light mainLight=GetMainLight();half shade=.96+.04*saturate(dot(normal,mainLight.direction));
                return half4(grain*_BaseColor.rgb*joint*shade,1);
            }
            ENDHLSL
        }
    }
}
