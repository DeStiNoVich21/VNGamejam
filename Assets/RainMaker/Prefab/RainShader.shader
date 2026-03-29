Shader "Custom/URP_RainShader"
{
    Properties
    {
        _MainTex ("Color (RGB) Alpha (A)", 2D) = "gray" {}
        _TintColor ("Tint Color (RGB)", Color) = (1, 1, 1, 1)
        _PointSpotLightMultiplier ("Point/Spot Light Multiplier", Range (0, 10)) = 2
        _DirectionalLightMultiplier ("Directional Light Multiplier", Range (0, 10)) = 1
        _InvFade ("Soft Particles Factor", Range(0.01, 3.0)) = 1.0
        _AmbientLightMultiplier ("Ambient light multiplier", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "IgnoreProjector" = "True" 
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite Off
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ SOFTPARTICLES_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                float4 projection : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _TintColor;
                half _DirectionalLightMultiplier;
                half _PointSpotLightMultiplier;
                half _AmbientLightMultiplier;
                half _InvFade;
            CBUFFER_END

            sampler2D _MainTex;

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                // Расчет освещения (упрощенный аналог LightForVertex)
                half3 ambient = SampleSH(float3(0,1,0)) * _AmbientLightMultiplier;
                Light mainLight = GetMainLight();
                
                // Directional Light
                half3 lightColor = ambient + (mainLight.color * _DirectionalLightMultiplier);

                // Применяем цвета
                output.color = half4(lightColor, 1.0) * input.color * _TintColor;
                
                // Коррекция альфы на основе яркости (как в оригинале)
                output.color.a *= (min(length(lightColor), _TintColor.a) / _TintColor.a);

                #if defined(SOFTPARTICLES_ON)
                output.projection = ComputeScreenPos(output.positionCS);
                #endif

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 col = tex2D(_MainTex, input.uv) * input.color;

                #if defined(SOFTPARTICLES_ON)
                float2 screenUV = input.projection.xy / input.projection.w;
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneZ = LinearEyeDepth(rawDepth, _ZBufferParams);
                float partZ = input.projection.w;
                float fade = saturate(_InvFade * (sceneZ - partZ));
                col.a *= fade;
                #endif

                return col;
            }
            ENDHLSL
        }
    }
    Fallback "Invisible/Transparent"
}