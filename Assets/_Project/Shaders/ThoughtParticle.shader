Shader "MindRush/ThoughtParticle"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1, 1, 1, 1)
        _EmissionColor ("Emission Color", Color) = (1, 0.5, 0, 1)
        _Intensity ("Intensity", Float) = 2.0
        _PulseSpeed ("Pulse Speed", Float) = 2.0
        _Size ("Base Size", Float) = 1.0
        _FadeInDuration ("Fade In Duration", Float) = 0.3
        _FadeOutDuration ("Fade Out Duration", Float) = 1.0
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
            #pragma multi_compile _ PARTICLES_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Properties.hlsl"

            #ifdef PARTICLES_INSTANCING_ON
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ParticleInstancing.hlsl"
            #endif

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 texcoord0 : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 vertexColor : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _EmissionColor;
                float _Intensity;
                float _PulseSpeed;
                float _Size;
                float _FadeInDuration;
                float _FadeOutDuration;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                #ifdef PARTICLES_INSTANCING_ON
                    ParticleInstanceData particleData = GetParticleInstanceData(input.positionOS, input.color);
                    output.positionCS = TransformObjectToHClip(particleData.positionOS);
                    output.vertexColor = particleData.color;
                    output.uv0 = input.texcoord0;
                #else
                    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                    output.positionCS = vertexInput.positionCS;
                    output.vertexColor = input.color;
                    output.uv0 = input.texcoord0;
                #endif

                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);

                // Sample texture
                half4 tex = TEXTURE2D_SAMPLE(_MainTex, sampler_MainTex, input.uv0);
                
                // Vertex color contains particle age/lifetime info
                // x = age, y = lifetime, z = random seed, w = start size
                float age = input.vertexColor.x;
                float lifetime = input.vertexColor.y;
                float randomSeed = input.vertexColor.z;
                
                // Normalized life (0 to 1)
                float lifeRatio = age / max(lifetime, 0.001);
                
                // Fade in/out
                float fadeIn = saturate(age / _FadeInDuration);
                float fadeOut = 1.0 - saturate((age - (lifetime - _FadeOutDuration)) / _FadeOutDuration);
                float alpha = fadeIn * fadeOut * input.vertexColor.w;
                
                // Pulse animation
                float pulse = sin(_Time.y * _PulseSpeed + randomSeed * 6.28318) * 0.5 + 0.5;
                
                // Color interpolation: start tint -> emission color
                half3 color = lerp(_Color.rgb, _EmissionColor.rgb, pulse * lifeRatio);
                
                // Size grows then shrinks
                float sizeMultiplier = sin(lifeRatio * 3.14159) * _Size;
                
                half3 emission = color * _Intensity * pulse;
                half3 finalColor = color + emission * 0.5;
                
                return half4(finalColor * tex.rgb, alpha * tex.a);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Particles/Unlit"
}