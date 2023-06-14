Shader "Custom/Silhouette" { 
	Properties {
		_Color ("Main Color", Color) = (1.0,1.0,1.0,1.0)
		_MainTex ("Base (RGB)", 2D) = "white" { }    
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
                o.color = float4(1,1,1,1);
            
                return o;
            }

            sampler2D _MainTex;
            
            half4 frag(v2f i) :COLOR {
                //can make these uniforms if you want it controllable.
                //setting color to 0 will make them a back silhouette, above mixes there colors.
                //setting alpha to 0 will remove the effect 1 will be a solid mix.
                float _AlphaAmount = 0.4;
                float _ColorAmount = 0.05;

                float4 c = tex2D(_MainTex, i.uv)*_ColorAmount;
                c.a=_AlphaAmount;
               return c;
            }
            ENDCG
		}
 
		Pass {
			Name "BASE"
			ZWrite On
			ZTest LEqual
            
			Blend SrcAlpha OneMinusSrcAlpha
               
            //js not sure why i need the to be x2 but it works otherwise teh texture is too dark some sort of colorspace thing?
			Material {
                 Diffuse (2,2,2,1)           
			}
			Lighting On
			SetTexture [_MainTex] {
				constantColor [_Color]
				Combine texture  * constant
			}
	
		}
	}
 
	Fallback "Diffuse"
}
