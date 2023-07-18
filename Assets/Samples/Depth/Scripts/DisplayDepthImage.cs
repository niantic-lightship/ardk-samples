using Niantic.Lightship.AR.Utilities;

namespace UnityEngine.XR.ARFoundation.Samples
{
    /// <summary>
    /// This component displays a picture-in-picture view of the specified AR image.
    /// </summary>
    public sealed class DisplayDepthImage : DisplayImage
    {
        /// <summary>
        /// Name of the max distance property in the shader.
        /// </summary>
        const string k_MaxDistanceName = "_MaxDistance";

        /// <summary>
        /// ID of the max distance property in the shader.
        /// </summary>
        static readonly int k_MaxDistanceId = Shader.PropertyToID(k_MaxDistanceName);

        [SerializeField]
        [Tooltip("The AROcclusionManager which will produce depth textures.")]
        AROcclusionManager m_OcclusionManager;

        [SerializeField]
        float m_MaxEnvironmentDistance = 8.0f;

        protected override void Awake()
        {
            base.Awake();
            Debug.Assert(m_OcclusionManager != null, "Missing occlusion manager component.");
        }

        /// <summary>
        /// Invoked when it is time to update the presentation.
        /// </summary>
        /// <param name="viewportWidth">The width of the portion of the screen the image will be rendered onto.</param>
        /// <param name="viewportHeight">The height of the portion of the screen the image will be rendered onto.</param>
        /// <param name="orientation">The orientation of the screen.</param>
        /// <param name="renderingMaterial">The material used to render the image.</param>
        /// <param name="image">The image to render.</param>
        /// <param name="displayMatrix">A transformation matrix to fit the image onto the viewport.</param>
        protected override void OnUpdatePresentation(int viewportWidth, int viewportHeight, ScreenOrientation orientation,
            Material renderingMaterial, out Texture image, out Matrix4x4 displayMatrix)
        {
            // Update the texture
            image = m_OcclusionManager.environmentDepthTexture;

            // Calculate the display matrix
            displayMatrix = image != null
                ? _CameraMath.CalculateDisplayMatrix(
                    image.width,
                    image.height,
                    viewportWidth,
                    viewportHeight,
                    orientation,
                    invertVertically: true)
                : Matrix4x4.identity;

            // Set custom attributes
            renderingMaterial.SetFloat(k_MaxDistanceId, m_MaxEnvironmentDistance);
        }
    }
}
