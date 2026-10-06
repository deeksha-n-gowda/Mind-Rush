Shader "MindRush/SynapseGlow"
{
    Properties
    {
        _Color ("Color", Color) = (0, 1, 1, 1)
        _EmissionColor ("Emission Color", Color) = (0, 1, 1, 1)
        _Intensity ("Intensity", Float) = 3.0
        _PulseSpeed ("Pulse Speed", Float) = 3.0
        _PulseWidth ("Pulse Width", Float) = 0.3
        _FlowSpeed ("Flow Speed", Float) = 2.0
        _NoiseScale ("Noise Scale", Float) = 1.0
        _Distortion ("Distortion", Float) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        LOD 100
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Universal2D"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 texcoord0 : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _EmissionColor;
                float _Intensity;
                float _PulseSpeed;
                float _PulseWidth;
                float _FlowSpeed;
                float _NoiseScale;
                float _Distortion;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv0 = input.texcoord0;
                return output;
            }

            float3 hash33(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                float3 x = frac(p.xxy + p.yxx);
                x *= x;
                float3 y = frac(x.xxy + x.yxx);
                return frac((x + y) * (x - y));
            }

            float noise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float3 w = hash33(i + float3(0,0,0));
                w = lerp(w, hash33(i + float3(1,0,0)), f.x);
                w = lerp(w, hash33(i + float3(0,1,0)), f.y);
                w = lerp(w, hash33(i + float3(0,0,1)), f.z);
                return dot(w, f.xyz + 0.5);
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                // Animated pulse along the synapse
                float pulse = sin(_Time.y * _PulseSpeed + input.positionWS.z * 0.2);
                pulse = pow(abs(pulse), 1.0 / _PulseWidth);
                
                // Flow animation
                float flow = frac(input.positionWS.z * 0.1 + _Time.y * _FlowSpeed);
                float flowMask = smoothstep(0.0, _PulseWidth, flow) * smoothstep(1.0, 1.0 - _PulseWidth, flow);
                
                // Organic noise distortion
                float noise = noise3D(input.positionWS * _NoiseScale + _Time.y * 0.5);
                float distortion = noise * _Distortion;
                
                // Combined intensity
                float intensity = (pulse * 0.7 + flowMask * 0.3) * _Intensity;
                intensity += distortion * 0.2;
                
                // Color with temperature variation (hotter = more yellow/white)
                float temp = pulse * 0.5 + flowMask * 0.5;
                half3 color = lerp(_Color.rgb, _EmissionColor.rgb, temp);
                color *= intensity;
                
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Particles/Lit"
}