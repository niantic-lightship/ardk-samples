Shader "Unlit/DepthDisplayDemo"
{
    Properties
    {
        _MainTex ("_MainTex", 2D) = "white" {}
        _DepthTex ("_DepthTex", 2D) = "green" {}
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off ZTest Always
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float3 texcoord : TEXCOORD0;
                float4 vertex : SV_POSITION;

            };

            // Sampler for the depth texture
            sampler2D _DepthTex;

            // Transform from screen space to depth texture space
            float4x4 _DepthTransform;

            inline float ConvertDistanceToDepth(float d)
            {
                // Clip any distances smaller than the near clip plane, and compute the depth value from the distance.
                return (d < _ProjectionParams.y) ? 0.0f : ((1.0f / _ZBufferParams.z) * ((1.0f / d) - _ZBufferParams.w));
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                // Apply the image transformation to the UV coordinates
                o.texcoord = mul(_DepthTransform, float4(v.uv, 1.0f, 1.0f)).xyz;
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Since the depth image transform may contain reprojection (for warping)
                // we need to convert the uv coordinates from homogeneous to cartesian
                float2 depth_uv = float2(i.texcoord.x / i.texcoord.z, i.texcoord.y / i.texcoord.z);

                // The depth value can be accessed by sampling the red channel
                // The values in the texture are metric eye depth (distance from the camera)
                float eyeDepth = tex2D(_DepthTex, depth_uv).r;

                // Convert the eye depth to a z-buffer value
                // The z-buffer value is a nonlinear value in the range [0, 1]
                float depth = ConvertDistanceToDepth(eyeDepth);

                // Use the z-value as color
#ifdef UNITY_REVERSED_Z
              return fixed4(depth, depth, depth, 1.0f);
#else
              return fixed4(1.0f - depth, 1.0f - depth, 1.0f - depth, 1.0f);
#endif
            }
            ENDCG
        }
    }
}
