Shader "MindRush/NeuralPathway"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.2, 0.1, 0.3, 1)
        _EmissionColor ("Emission Color", Color) = (0.5, 0.2, 0.8, 1)
        _EmissionIntensity ("Emission Intensity", Float) = 1.5
        _PulseSpeed ("Pulse Speed", Float) = 2.0
        _PulseIntensity ("Pulse Intensity", Float) = 0.3
        _Glossiness ("Smoothness", Range(0,1)) = 0.7
        _Metallic ("Metallic", Range(0,1)) = 0.1
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalScale ("Normal Scale", Float) = 1.0
        _FlowSpeed ("Energy Flow Speed", Float) = 1.0
        _FlowIntensity ("Energy Flow Intensity", Float) = 0.5
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
            Blend SrcAlpha OneMinusSrcAlpha

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
                float2 texcoord1 : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float3 positionWS : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor;
                float _EmissionIntensity;
                float _PulseSpeed;
                float _PulseIntensity;
                float _Glossiness;
                float _Metallic;
                float _NormalScale;
                float _FlowSpeed;
                float _FlowIntensity;
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
                output.uv1 = input.texcoord1;
                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                // Pulsing emission based on time and world position
                float pulse = sin(_Time.y * _PulseSpeed + input.positionWS.z * 0.1) * 0.5 + 0.5;
                float emissionPulse = 1.0 + pulse * _PulseIntensity;

                // Energy flow along Z axis (forward direction)
                float flow = frac(input.positionWS.z * 0.05 + _Time.y * _FlowSpeed);
                float flowGlow = smoothstep(0.5, 0.5 + _FlowIntensity, flow) * _FlowIntensity;

                // Normal mapping
                float3 normalTS = UnpackNormal(TEXTURE2D_SAMPLE(_NormalMap, sampler_NormalMap, input.uv0));
                normalTS = NormalScale(normalTS, _NormalScale);
                float3 normalWS = normalize(input.normalWS + normalTS.xyz * 0.5);

                // Base color with subtle variation
                half3 baseColor = _BaseColor.rgb;
                
                // Add myelin sheath highlights (periodic bright bands)
                float myelin = sin(input.positionWS.z * 0.8) * 0.5 + 0.5;
                baseColor += myelin * 0.05;

                // Emission
                half3 emission = _EmissionColor.rgb * _EmissionIntensity * emissionPulse;
                emission += flowGlow * _EmissionColor.rgb * 2.0;

                // Lighting
                float3 albedo = baseColor;
                float metallic = _Metallic;
                float smoothness = _Glossiness;
                float occlusion = 1.0;

                // Simple lighting for Universal 2D
                float3 lightColor = _LightColor0.rgb;
                float NdotL = max(0, dot(normalWS, _LightDir0));
                float3 diffuse = albedo * lightColor * NdotL * (1 - metallic);
                
                // Specular
                float3 halfDir = normalize(_LightDir0 + input.viewDirWS);
                float NdotH = max(0, dot(normalWS, halfDir));
                float specular = pow(NdotH, smoothness * 100) * lightColor * (1 - metallic) * 0.5;

                half3 color = diffuse + specular + emission * 0.1;
                color = saturate(color);

                return half4(color, _BaseColor.a);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}