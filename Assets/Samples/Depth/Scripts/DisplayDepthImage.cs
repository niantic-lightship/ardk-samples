// Copyright 2023 Niantic, Inc. All Rights Reserved.
using Niantic.Lightship.AR.Utilities;
using UnityEngine.XR.ARFoundation;
using UnityEngine;

namespace Niantic.Lightship.AR.Samples
{
    // This component displays a picture-in-picture view of the specified AR image.
    public sealed class DisplayDepthImage : DisplayImage
    {

        const string k_MaxDistanceName = "_MaxDistance";

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

        protected override void OnUpdatePresentation(int viewportWidth, int viewportHeight, ScreenOrientation orientation,
            Material renderingMaterial, out Texture image, out Matrix4x4 displayMatrix)
        {
            // Update the texture
            image = m_OcclusionManager.environmentDepthTexture;

            // Calculate the display matrix
            displayMatrix = image != null
                ? CameraMath.CalculateDisplayMatrix(
                    image.width,
                    image.height,
                    viewportWidth,
                    viewportHeight,
                    orientation,
                    invertVertically: true)
                : Matrix4x4.identity;

            // Set custom attributes
            renderingMaterial.SetFloat(k_MaxDistanceId, m_MaxEnvironmentDistance);
            renderingMaterial.SetTexture("_DepthTex", image);

        }
    }
}
