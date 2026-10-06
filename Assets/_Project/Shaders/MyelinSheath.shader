Shader "MindRush/MyelinSheath"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.9, 0.85, 0.95, 1)
        _EmissionColor ("Emission Color", Color) = (0.3, 0.1, 0.5, 1)
        _EmissionIntensity ("Emission Intensity", Float) = 1.0
        _SegmentWidth ("Segment Width", Float) = 2.0
        _GapWidth ("Gap Width (Node of Ranvier)", Float) = 0.3
        _PulseSpeed ("Pulse Speed", Float) = 1.5
        _Glossiness ("Smoothness", Range(0,1)) = 0.9
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalScale ("Normal Scale", Float) = 0.5
        _Iridescence ("Iridescence", Float) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "Universal2D"
            Tags { "LightMode" = "Universal2D" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Properties.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 texcoord0 : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor;
                float _EmissionIntensity;
                float _SegmentWidth;
                float _GapWidth;
                float _PulseSpeed;
                float _Glossiness;
                float _Metallic;
                float _NormalScale;
                float _Iridescence;
            CBUFFER_END

            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = TransformNormal(input.normalOS, 1.0);
                output.viewDirWS = normalize(_WorldSpaceCameraPos - output.positionWS);
                output.uv0 = input.texcoord0;
                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                // Myelin segments (wrapped) with Nodes of Ranvier (gaps)
                float segmentPattern = sin(input.positionWS.z * (6.28318 / _SegmentWidth));
                float gapMask = step(abs(segmentPattern), _GapWidth);
                
                // Nodes of Ranvier pulse with action potential
                float nodePulse = sin(_Time.y * _PulseSpeed + input.positionWS.z * 0.5) * 0.5 + 0.5;
                float nodeGlow = gapMask * nodePulse * _EmissionIntensity * 2.0;
                
                // Myelin sheath has subtle shimmer
                float myelinShimmer = (sin(input.positionWS.z * 0.5 + _Time.y * 0.3) * 0.5 + 0.5) * 0.05;
                
                // Normal mapping
                float3 normalTS = UnpackNormal(TEXTURE2D_SAMPLE(_NormalMap, sampler_NormalMap, input.uv0));
                normalTS = NormalScale(normalTS, _NormalScale);
                float3 normalWS = normalize(input.normalWS + normalTS.xyz * 0.3);

                // Base color - white myelin with slight translucency
                half3 baseColor = _BaseColor.rgb;
                baseColor += myelinShimmer;
                
                // Iridescent sheen (thin-film interference)
                float iridescence = _Iridescence * (dot(input.viewDirWS, normalWS) * 0.5 + 0.5);
                baseColor += iridescence * half3(0.1, 0.2, 0.4);

                // Emission at nodes of Ranvier
                half3 emission = _EmissionColor.rgb * nodeGlow;

                // Lighting
                float3 albedo = baseColor;
                float metallic = _Metallic;
                float smoothness = _Glossiness;
                float occlusion = 1.0;

                float3 lightColor = _LightColor0.rgb;
                float NdotL = max(0, dot(normalWS, _LightDir0));
                float3 diffuse = albedo * lightColor * NdotL * (1 - metallic);
                
                float3 halfDir = normalize(_LightDir0 + input.viewDirWS);
                float NdotH = max(0, dot(normalWS, halfDir));
                float specular = pow(NdotH, smoothness * 100) * lightColor * (1 - metallic) * 0.3;

                half3 color = diffuse + specular + emission * 0.1;
                color = saturate(color);

                return half4(color, _BaseColor.a);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}