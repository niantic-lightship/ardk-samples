// Unlit Transparent Vertex Color Shader
//
// This shader renders a mesh without being affected by scene lighting.
// - The final color is determined by the mesh's vertex colors.
// - A Grayscale effect can be toggled.
// - Transparency is controlled by a single master Alpha slider.
// - It uses a two-pass approach to correctly render complex transparent meshes
//   that have overlapping parts (solves Z-sorting/depth issues).

Shader "Custom/UnlitVertexColorTransparent"
{
    Properties
    {
        // Master transparency control. 1 is opaque, 0 is fully transparent.
        _Alpha ("Alpha", Range(0, 1)) = 1.0
        // A toggle to enable the grayscale effect. 0 is off, 1 is on.
        _Grayscale ("Grayscale", Int) = 0
    }

    SubShader
    {
        // Set up tags for the transparency rendering queue.
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        // PASS 1: Depth Pre-Pass
        // This pass writes only to the depth buffer (Z-buffer). It draws no color.
        // Its purpose is to establish the shape of the mesh first, so that
        // the color pass can correctly sort transparent overlapping surfaces.
        Pass
        {
            ZWrite On
            ColorMask 0 // Don't write to any color channels (R, G, B, or A).
            Cull Back   // Cull back-facing polygons for performance.
        }

        // PASS 2: Color Pass
        // This pass renders the final color of the object.
        Pass
        {
            ZWrite Off              // Don't write to the depth buffer (it's already been written).
            Blend SrcAlpha OneMinusSrcAlpha // Standard alpha blending.
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // Variables from the Properties block.
            half _Alpha;
            int _Grayscale;

            // Data passed from the vertex shader to the fragment shader.
            struct v2f
            {
                float4 pos   : SV_POSITION; // Vertex position in clip space.
                float4 color : COLOR;       // Vertex color, processed by the vertex shader.
            };

            // VERTEX SHADER
            // Processes each vertex of the mesh.
            v2f vert(appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                // Apply grayscale effect if enabled.
                if (_Grayscale == 1)
                {
                    float grayscale = dot(v.color.rgb, float3(0.299, 0.587, 0.114));
                    o.color.rgb = grayscale;
                }
                else
                {
                    o.color.rgb = v.color.rgb;
                }

                // Pass the original vertex alpha. It might be useful for other effects,
                // even though we ignore it for final transparency in this shader.
                o.color.a = v.color.a;

                return o;
            }

            // FRAGMENT SHADER
            // Processes each pixel on the screen.
            fixed4 frag(v2f i) : SV_Target
            {
                // The final color is simply the interpolated vertex color (already grayscaled if needed).
                fixed3 finalColor = i.color.rgb;

                // The final alpha is determined *only* by the material's _Alpha property,
                // ignoring the mesh's own vertex alpha (i.color.a) to prevent the object
                // from becoming invisible if the vertex alpha data is 0.
                fixed finalAlpha = _Alpha;

                return fixed4(finalColor, finalAlpha);
            }
            ENDCG
        }
    }
    FallBack "Transparent/VertexLit"
}