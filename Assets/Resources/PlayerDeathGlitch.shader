Shader "Link/Player Death Glitch"
{
    Properties
    {
        [PerRendererData] _MainTex ("Death Frame", 2D) = "white" {}
        _Progress ("Progress", Range(0, 1)) = 0
        _PowerOn ("Power On", Float) = 0
        _ShutdownStart ("Shutdown Start", Range(0, 1)) = 0.74
        _ColorSeparation ("Color Separation", Range(0, 2)) = 1
        _ShakeStrength ("Shake Strength", Range(0, 2)) = 1
        _ShadowDarkening ("Shadow Darkening", Range(0, 1)) = 0.9
    }

    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Progress;
            float _PowerOn;
            float _ShutdownStart;
            float _ColorSeparation;
            float _ShakeStrength;
            float _ShadowDarkening;

            struct Attributes
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.position);
                output.uv = input.uv;
                return output;
            }

            float Hash(float2 value)
            {
                return frac(sin(dot(value, float2(127.1, 311.7))) * 43758.5453);
            }

            float2 ScreenAperture(float closure)
            {
                float height = lerp(0.5, 0.0025, smoothstep(0.0, 0.65, closure));
                float width = 0.5 * (1.0 - smoothstep(0.62, 1.0, closure));
                return float2(width, height);
            }

            fixed4 Frag(Varyings input) : SV_Target
            {
                bool starting = _PowerOn > 0.5;
                if (starting && _Progress >= 1.0)
                    return fixed4(0, 0, 0, 0);
                float closure = starting ? 1.0 - _Progress
                    : saturate((_Progress - _ShutdownStart) / max(0.001, 1.0 - _ShutdownStart));
                if (closure >= 1.0)
                    return fixed4(0, 0, 0, 1);

                float2 aperture = ScreenAperture(closure);
                float2 centered = input.uv - 0.5;
                if (any(abs(centered) > aperture))
                    return fixed4(0, 0, 0, 1);

                float lineGlow = 0.32 * smoothstep(0.58, 0.66, closure) * (1.0 - smoothstep(0.82, 1.0, closure));
                if (starting)
                    return fixed4(0.34, 0.59, 0.86, lineGlow);

                float2 uv = centered / max(aperture * 2.0, 0.001) + 0.5;
                float frame = floor(_Progress * 34.0);
                float2 shake = float2(Hash(float2(frame, 17.0)), Hash(float2(frame, 41.0))) - 0.5;
                uv += shake * float2(0.008, 0.004) * _ShakeStrength;
                float band = floor(uv.y * 27.0);
                float tear = step(0.91, Hash(float2(band, frame)));
                uv.x += tear * (Hash(float2(frame, band + 8.0)) - 0.5) * 0.021 * _ShakeStrength;
                float split = (0.0025 + 0.002 * Hash(float2(frame, 59.0)) + tear * 0.002) * _ColorSeparation;
                float3 color = tex2D(_MainTex, saturate(uv)).rgb;
                const float3 luminance = float3(0.2126, 0.7152, 0.0722);
                float brightness = dot(color, luminance);
                float redEdge = max(0.0, dot(tex2D(_MainTex, saturate(uv - float2(split, 0))).rgb, luminance) - brightness);
                float blueEdge = max(0.0, dot(tex2D(_MainTex, saturate(uv + float2(split, 0))).rgb, luminance) - brightness);
                color *= lerp(1.0, smoothstep(0.055, 0.4, brightness), _ShadowDarkening);
                color += float3(redEdge * 0.28, 0.0, blueEdge * 0.32) * saturate(_ColorSeparation);
                color = lerp(color, float3(0.34, 0.59, 0.86), lineGlow);
                return fixed4(color, 1);
            }
            ENDCG
        }
    }
}
