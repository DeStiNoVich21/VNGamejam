Shader "Custom/CyberClub_Advanced_Floor"
{
    Properties
    {
        [Header(Base Textures)]
        _MainTex ("Surface Detail (Roughness/Metal)", 2D) = "white" {}
        _EmissionMap ("LED Mask (Alpha defines LED zones)", 2D) = "white" {}
        
        [Header(Colors)]
        [HDR]_ColorA ("Neon Color A (Primary)", Color) = (0, 0.5, 1, 1)
        [HDR]_ColorB ("Neon Color B (Secondary)", Color) = (1, 0, 0.5, 1)
        _BaseDarkness ("Base Floor Darkness", Range(0, 1)) = 0.1

        [Header(Animation Settings)]
        _GridScale ("Grid Scale", Float) = 20.0
        _PulseSpeed ("Pulse Speed", Float) = 0.5
        _WaveSize ("Wave Spread", Range(0.1, 5)) = 1.0
        
        [Header(Glitch)]
        _GlitchChance ("Glitch Frequency", Range(0, 1)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _EmissionMap;
            float4 _ColorA, _ColorB;
            float _GridScale, _PulseSpeed, _WaveSize, _BaseDarkness, _GlitchChance;

            float hash(float2 p) {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            Varyings vert(Attributes input) {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target {
                // 1. Используем мировые координаты для сетки (World Space)
                // Это гарантирует, что на огромном полу плитки будут одинакового размера
                float2 gridUV = input.worldPos.xz * _GridScale;
                float2 id = floor(gridUV);
                float2 fuv = frac(gridUV);

                // 2. Текстуры поверхности
                float4 surface = tex2D(_MainTex, input.uv);
                float4 ledMask = tex2D(_EmissionMap, input.uv);

                // 3. Создаем "Волну" цвета, которая идет через весь пол
                float time = _Time.y * _PulseSpeed;
                // Смешиваем позицию X и Z для диагонального движения
                float wave = sin((id.x + id.y) * _WaveSize + time); 
                wave = wave * 0.5 + 0.5; // Приводим к диапазону 0..1

                // 4. Глитч-эффект (случайное мигание отдельных плиток)
                float flicker = hash(id + floor(_Time.y * 15.0));
                float glitch = step(1.0 - _GlitchChance, flicker);

                // 5. Смешивание цветов (Lerp)
                // Основной цвет плавно перетекает из A в B по всему залу
                half3 dynamicColor = lerp(_ColorA.rgb, _ColorB.rgb, wave);
                
                // Добавляем эффект "выключенных" или мерцающих панелей
                dynamicColor *= (0.3 + 0.7 * step(0.2, hash(id))); 
                if(glitch > 0.5) dynamicColor *= 2.0; // Вспышка при глитче

                // 6. Финальный результат
                // Затемняем основу, чтобы неон выделялся
                half3 finalRGB = surface.rgb * _BaseDarkness;
                
                // Накладываем неон только там, где позволяет маска (или сетка)
                float border = smoothstep(0.05, 0.1, fuv.x) * smoothstep(0.95, 0.9, fuv.x) *
                               smoothstep(0.05, 0.1, fuv.y) * smoothstep(0.95, 0.9, fuv.y);
                
                finalRGB += dynamicColor * border * ledMask.r;

                return half4(finalRGB, 1.0);
            }
            ENDHLSL
        }
    }
}