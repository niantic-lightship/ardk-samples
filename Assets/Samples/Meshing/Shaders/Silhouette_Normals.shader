Shader "Custom/Silhouette_Normals" { 
	Properties {
		_Color ("Main Color", Color) = (1.0,1.0,1.0,1.0)
		_MainTex ("Base (RGB)", 2D) = "white" { } 
		_AlphaAmount ("Alpha Amount", float) = 0.5
		_AlphaAmount2 ("Alpha Amount", float) = 0.5
		_ColorAmount ("Color Amount", float) = 1.0
		_ColorAmount2 ("Color Amount 2", float) = 0.5
	}
 
    SubShader {
		Pass {
			Name "GHOST"
			Cull Back
			ZWrite Off           
			ZTest GEqual

            //if you want to remove the over draw for overlapping items add this stencil buffer.
            Stencil {
                Ref 1
                Comp GEqual
                Pass Invert
            }
            
            Blend SrcAlpha OneMinusSrcAlpha
	       
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
    
            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f {
                float4 pos : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                
            };
            
            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
            	o.color = float4(v.normal,1);
                return o;
            }
            uniform float _AlphaAmount;
            uniform float _AlphaAmount2;
            
			uniform float _ColorAmount;
            uniform float _ColorAmount2;
            
            sampler2D _MainTex;
            //this just needs to overdraw so white is fine!
            half4 frag(v2f i) :COLOR {
            	float4 c = _ColorAmount2*i.color;
                c.a=_AlphaAmount2;
				return c;
            }
            ENDCG
		}
 
		Pass {
			Name "BASE"
			ZWrite On
			ZTest LEqual
            
			Blend SrcAlpha OneMinusSrcAlpha
			
			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
    
            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

			sampler2D _MainTex;
            
            struct v2f {
                float4 pos : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                
            };

			  v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = float4(v.normal,1);
            
                return o;
            }

			uniform float4 _Color;
			uniform float _AlphaAmount;
			uniform float _ColorAmount;
			
			
			half4 frag(v2f i) :COLOR {
				float4 c = i.color*_ColorAmount;
                c.a=_AlphaAmount;
				return c;
            }
			ENDCG

	
		}
	}
 
	Fallback "Diffuse"
}
