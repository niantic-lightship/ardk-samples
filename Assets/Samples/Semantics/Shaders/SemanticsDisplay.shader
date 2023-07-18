Shader "Unlit/SemanticsDisplay"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "black" {}
    }
    // URP SubShader
    SubShader
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.universal": "12.0"
        }

        Tags
        {
            "Queue" = "Geometry"
            "RenderType" = "Opaque"
            "ForceNoShadowCasting" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Cull Off
            ZTest Always
            ZWrite Off
            Lighting Off
            LOD 100
            Tags
            {
                "LightMode" = "UniversalForward"
            }

        HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float3 position : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 texcoord : TEXCOORD0;
            };

            struct fragment_output
            {
                half4 color : SV_Target;
            };

            float4x4 _DisplayMatrix;

            v2f vert (appdata v)
            {
                v2f o;
                o.position = TransformObjectToHClip(v.position);
                o.texcoord = mul(_DisplayMatrix, float4(v.texcoord, 1.0f, 1.0f)).xyz;
                return o;
            }

            half3 HSVtoRGB(half3 arg1)
            {
                half4 K = half4(1.0h, 2.0h / 3.0h, 1.0h / 3.0h, 3.0h);
                half3 P = abs(frac(arg1.xxx + K.xyz) * 6.0h - K.www);
                return arg1.z * lerp(K.xxx, saturate(P - K.xxx), arg1.y);
            }

            TEXTURE2D_FLOAT(_MainTex);
            SAMPLER(sampler_MainTex);

            fragment_output frag (v2f i)
            {
                // Sample semantics
                float2 uv = float2(i.texcoord.x / i.texcoord.z, i.texcoord.y / i.texcoord.z);
                float confidence = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).r;

                half hue = lerp(0.70h, -0.15h, saturate(confidence));
                if (hue < 0.0h)
                {
                    hue += 1.0h;
                }
                half3 color = half3(hue, 0.9h, 0.6h);

                fragment_output o;
                o.color = half4(HSVtoRGB(color), 1.0h);
                return o;
            }

            ENDHLSL
        }
    }

    // Built-in Render Pipeline SubShader
    SubShader
    {
        Tags
        {
            "Queue" = "Geometry"
            "RenderType" = "Opaque"
            "ForceNoShadowCasting" = "True"
        }

        Pass
        {
            Cull Off
            ZTest Always
            ZWrite Off
            Lighting Off
            LOD 100
            Tags
            {
                "LightMode" = "Always"
            }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #define TransformObjectToHClip UnityObjectToClipPos

            #define DECLARE_TEXTURE2D_FLOAT(texture) UNITY_DECLARE_TEX2D_FLOAT(texture)
            #define DECLARE_SAMPLER_FLOAT(sampler)
            #define SAMPLE_TEXTURE2D(texture,sampler,texcoord) UNITY_SAMPLE_TEX2D(texture,texcoord)

            struct appdata
            {
                float3 position : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 texcoord : TEXCOORD0;
            };

            struct fragment_output
            {
                half4 color : SV_Target;
            };

            float4x4 _DisplayMatrix;

            v2f vert (appdata v)
            {
                v2f o;
                o.position = TransformObjectToHClip(v.position);
                o.texcoord = mul(_DisplayMatrix, float4(v.texcoord, 1.0f, 1.0f)).xyz;
                return o;
            }

            half3 HSVtoRGB(half3 arg1)
            {
                half4 K = half4(1.0h, 2.0h / 3.0h, 1.0h / 3.0h, 3.0h);
                half3 P = abs(frac(arg1.xxx + K.xyz) * 6.0h - K.www);
                return arg1.z * lerp(K.xxx, saturate(P - K.xxx), arg1.y);
            }

            DECLARE_TEXTURE2D_FLOAT(_MainTex);
            DECLARE_SAMPLER_FLOAT(sampler_MainTex);

            fragment_output frag (v2f i)
            {
                // Sample semantics
                float2 uv = float2(i.texcoord.x / i.texcoord.z, i.texcoord.y / i.texcoord.z);
                float confidence = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).r;

                half hue = lerp(0.70h, -0.15h, saturate(confidence));
                if (hue < 0.0h)
                {
                    hue += 1.0h;
                }
                half3 color = half3(hue, 0.9h, 0.6h);

                fragment_output o;
                o.color = half4(HSVtoRGB(color), 1.0h);
                return o;
            }

            ENDHLSL
        }
    }
}
