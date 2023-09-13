Shader "Unlit/DepthFit"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "black" {}
    }
    
    // Built-in Render Pipeline SubShader
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "ForceNoShadowCasting" = "True"
        }

        Pass
        {
            Cull Off
            Lighting Off
            ZWrite Off
            ZTest [unity_GUIZTestMode]
            Blend SrcAlpha OneMinusSrcAlpha
            LOD 100
            Tags
            {
                "LightMode" = "Always"
            }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_local __ ANDROID_PLATFORM

            #include "UnityCG.cginc"

            // Display transform
            float4x4 _DisplayMatrix;

            // Image sampler
            sampler2D _MainTex;

            struct appdata
            {
                float3 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct fragment_output
            {
                float4 color : SV_Target;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
#if ANDROID_PLATFORM
                o.texcoord = mul(float3(v.texcoord.x, 1.0f - v.texcoord.y, 1.0f), _DisplayMatrix).xy;
#else
                o.texcoord = mul(float3(v.texcoord, 1.0f), _DisplayMatrix).xy;
#endif
                return o;
            }

            fragment_output frag (v2f i)
            {
#if !STEREO_MULTIVIEW_ON                
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
#endif                

                // Sample the environment depth (in meters).
                float depth = tex2D(_MainTex, i.texcoord).r;

                // Write disparity to the color channels for debug purposes
                const float MAX_VIEW_DISP = 4.0f;
                const float scaledDisparity = 1.0f / depth;
                const float normDisparity = scaledDisparity / MAX_VIEW_DISP;

                fragment_output o;
                o.color = float4(normDisparity, normDisparity, normDisparity, 0.75f);
                return o;
            }

            ENDHLSL
        }
    }
}
