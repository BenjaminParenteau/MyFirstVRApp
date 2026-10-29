Shader "High Stakes/Treasury Surface"
{
    Properties
    {
        _BaseMap("Albedo",2D)="white"{}
        _BaseColor("Tint",Color)=(1,1,1,1)
        _BumpMap("Normal",2D)="bump"{}
        _BumpScale("Normal strength",Range(0,2))=.35
        _Metallic("Metallic",Range(0,1))=0
        _Smoothness("Smoothness",Range(0,1))=.4
        _EmissionColor("Emission",Color)=(0,0,0,0)
        _Environment("Treasury reflection",Cube)=""{}
        _KeyPosition("Key light position",Vector)=(41.5,2.65,2.7,0)
        _FillPosition("Fill light position",Vector)=(45,2.65,4.3,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "TreasuryForward"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            TEXTURECUBE(_Environment); SAMPLER(sampler_Environment);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST,_BaseColor,_EmissionColor,_KeyPosition,_FillPosition;
            half _Metallic,_Smoothness,_BumpScale;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 tangentOS:TANGENT;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;half4 tangentWS:TEXCOORD2;float2 uv:TEXCOORD3;half fog:TEXCOORD4;UNITY_VERTEX_OUTPUT_STEREO };
            Varyings Vert(Attributes input)
            {
                Varyings output=(Varyings)0;UNITY_SETUP_INSTANCE_ID(input);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs pos=GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS=pos.positionCS;output.positionWS=pos.positionWS;
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);
                output.tangentWS=half4(TransformObjectToWorldDir(input.tangentOS.xyz),input.tangentOS.w*GetOddNegativeScale());
                output.uv=TRANSFORM_TEX(input.uv,_BaseMap);output.fog=ComputeFogFactor(pos.positionCS.z);return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 n=normalize(input.normalWS),t=normalize(input.tangentWS.xyz),b=cross(n,t)*input.tangentWS.w;
                half3 normalTS=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,input.uv),_BumpScale);
                n=normalize(t*normalTS.x+b*normalTS.y+n*normalTS.z);
                half3 albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).rgb*_BaseColor.rgb;
                half alpha=1;BRDFData brdf;InitializeBRDFData(albedo,_Metallic,half3(.04,.04,.04),_Smoothness,alpha,brdf);
                half3 view=GetWorldSpaceNormalizeViewDir(input.positionWS);
                float3 key=_KeyPosition.xyz-input.positionWS,fill=_FillPosition.xyz-input.positionWS;
                // Self-contained local light rig: works with the project's additional-lights-disabled Quest renderer.
                half3 color=LightingPhysicallyBased(brdf,half3(2.6,2.35,1.95),normalize(key),rcp(1+.12*dot(key,key)),n,view);
                color+=LightingPhysicallyBased(brdf,half3(1.65,2.0,2.5),normalize(fill),rcp(1+.12*dot(fill,fill)),n,view);
                color+=brdf.diffuse*half3(.24,.27,.30);
                half3 environment=SAMPLE_TEXTURECUBE_LOD(_Environment,sampler_Environment,reflect(-view,n),(1-_Smoothness)*5).rgb;
                half fresnel=Pow4(1-saturate(dot(n,view)));
                color+=environment*lerp(brdf.specular,half3(1,1,1),fresnel)*.8;
                color+=_EmissionColor.rgb;
                return half4(MixFog(color,input.fog),1);
            }
            ENDHLSL
        }
    }
}
