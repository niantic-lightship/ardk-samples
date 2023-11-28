Shader "Custom/YetiShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Color1 ("Color1", Color) = (1,1,1,1)
        _Color2 ("Color2", Color) = (1,1,1,1)
        _Color3 ("Color3", Color) = (1,1,1,1)
        
        _ColorMask ("ColorMask", 2D) = "white" {}
        _BWTex ("BWTex", 2D) = "white" {}
        
        
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _BumpMap ("Bumpmap", 2D) = "bump" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Geometry+200" }
        LOD 200

        Pass
        {
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            
            // Use shader model 3.0 target, to get nicer looking lighting
            #pragma target 3.0
            sampler2D _MainTex;
            sampler2D _BumpMap;
            sampler2D _ColorMask;
            sampler2D _BWTex;
            
            #include "UnityCG.cginc"
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
        
            half _Glossiness;
            half _Metallic;
            fixed4 _Color;
            fixed4 _Color1;
            fixed4 _Color2;
            fixed4 _Color3;
            
            v2f vert (appdata_base d)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(d.vertex);
                o.uv = d.texcoord;
              
                return o;
            }
        
            fixed4 frag (v2f i) : SV_Target
            {
                // Albedo comes from a texture tinted by color
                fixed4 mask = tex2D (_ColorMask, i.uv);
                fixed4 c = tex2D (_MainTex, i.uv);
                fixed4 bw = tex2D (_BWTex, i.uv);
                
                //these are overrides full replace the color in this region
                float4 fur = bw*mask.r * _Color;
                float4 face = bw*mask.b * _Color1;
                //this is a tint of the existing color not an override
                float4 bag = c*mask.g * _Color2;
                
                //combine the bits
                float4 colors = fur+face+bag; 
                float full_mask = clamp(mask.r+mask.b+mask.g,0,1);
                float full_mask_inv = 1.0-clamp(mask.r+mask.b+mask.g,0,1);
                
                //composite bg with character.
                c = ((c*full_mask_inv )+ face + fur+bag);
                return c;
         
            }
            ENDCG
        }
        pass
        {
            Tags{ "LightMode" = "ShadowCaster" }
            CGPROGRAM
            #pragma vertex VSMain
            #pragma fragment PSMain
 
            float4 VSMain (float4 vertex:POSITION) : SV_POSITION
            {
                return UnityObjectToClipPos(vertex);
            }
 
            float4 PSMain (float4 vertex:SV_POSITION) : SV_TARGET
            {
                return 0;
            }
           
            ENDCG      
        }
    }
}