using UnityEngine.UI;

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

        protected override void OnUpdateImage(RawImage image)
        {
            Debug.Assert(m_OcclusionManager != null, "No occlusion manager.");
            image.texture = m_OcclusionManager.environmentDepthTexture;
            image.material.SetFloat(k_MaxDistanceId, m_MaxEnvironmentDistance);
        }
    }
}
