Shader "Custom/TriPlanar"
{
    //simple triplanar shader
    //use this shader on a mesh prefab in order to augment the world.
    //swap the textures to make different environments, like desert, caves etc
    Properties
    {
        _GroundTex ("_GroundTex (RGB)", 2D) = "green" {}
        _WallTex ("_WallTex (RGB)", 2D) = "blue" {}
        _CeilingTex ("_CeilingTex (RGB)", 2D) = "red" {}
        _Alpha ("Alpha", float) = 0.9
    }
    SubShader
    {
        Blend SrcAlpha OneMinusSrcAlpha
        ZTest LEqual
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };
            
            #pragma target 3.0

            sampler2D _GroundTex;
            sampler2D _WallTex;
            sampler2D _CeilingTex;

            uniform float _Alpha;
            
            float4 Sample_TriPlanar( sampler2D s, in float3 p, in float3 n, in float k )
            {
                float2 uv_x = p.yz;
                float2 uv_y = p.zx;
                float2 uv_z = p.xy;
                
                if (n.x < 0) {
		            uv_x.x = -uv_x.x;
	            }
	            if (n.y < 0) {
		            uv_y.x = -uv_y.x;
	            }
	            if (n.z >= 0) {
		            uv_z.x = -uv_z.x;
	            }
                
	            float4 x = tex2D( s, uv_x );
	            float4 y = tex2D( s, uv_y );
	            float4 z = tex2D( s, uv_z );
                
                float3 m = pow( abs(n), float3(k,k,k) );
	            return (x*m.x + y*m.y + z*m.z) / (m.x + m.y + m.z);
            }
            
            v2f vert (float4 vertex : POSITION, float3 normal : NORMAL,  float2 uv : TEXCOORD0)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);
                float3 worldPos = mul(unity_ObjectToWorld, vertex).xyz;
                o.worldPos = worldPos;

                float3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
                
                float3 worldNormal = UnityObjectToWorldNormal(normal);
                o.worldNormal = worldNormal;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                //project our texture onto the mesh using tri planer.
                float3 WorldNormal = normalize( mul(float4(i.worldNormal, 0.0), unity_ObjectToWorld).xyz);
                
                float4 ground = Sample_TriPlanar (_GroundTex, i.worldPos, i.worldNormal, 0.8);
                float4 wall =   Sample_TriPlanar (_WallTex,   i.worldPos, i.worldNormal, 0.8);
                float4 ceiling =   Sample_TriPlanar (_CeilingTex,   i.worldPos, i.worldNormal, 0.8);
                
                //this part of the shader just selects which tiled texture to use based on the surface normal
                //if y is negative its pointing down so its a ceiling
                if(i.worldNormal.y<0)
                {
                    ground = ceiling;
                }

                //blend ground to wall based on how much the normal points up.
                float4 col = lerp(wall,ground,abs(i.worldNormal.y));
                col.a = _Alpha;
                return col;
            }
            ENDCG
        }
    }
}
