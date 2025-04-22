// Copyright 2022-2025 Niantic.
using System.Text;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;
using UnityEngine;

namespace Niantic.Lightship.AR.Samples
{
    public abstract class DisplayImage : MonoBehaviour
    {
        // Name of the display rotation matrix in the shader.
        const string k_DisplayMatrixName = "_DisplayMatrix";

        private readonly int k_DisplayMatrix = Shader.PropertyToID(k_DisplayMatrixName);

        protected readonly StringBuilder m_StringBuilder = new();

        protected ScreenOrientation m_CurrentScreenOrientation;

        [SerializeField]
        [Tooltip("The ARCameraManager which will produce camera frame events.")]
        ARCameraManager m_CameraManager;

        [SerializeField]
        [Tooltip("Raw Image UI element for display.")]
        protected RawImage m_RawImage;

        [SerializeField]
        [Tooltip("Material using the shader.")]
        Material m_Material;

        [SerializeField]
        [Tooltip("UI Text field for image info")]
        Text m_ImageInfo;

        private Camera m_camera;

        protected virtual void Awake()
        {
            // Acquire a reference to the rendering camera
            m_camera = m_CameraManager.GetComponent<Camera>();

            // Get the current screen orientation, and update the raw image UI
            m_CurrentScreenOrientation = Screen.orientation;
        }

        protected virtual void OnEnable()
        {
            UpdateRawImage();
        }

        protected virtual void Update()
        {
            Debug.Assert(m_RawImage != null, "no raw image");

            // If the raw image needs to be updated because of a device orientation change or because of a texture
            // aspect ratio difference, then update the raw image with the new values.
            if (m_CurrentScreenOrientation != Screen.orientation)
            {
                m_CurrentScreenOrientation = Screen.orientation;
                UpdateRawImage();
            }

            // Update the image
            var sizeDelta = m_RawImage.rectTransform.sizeDelta;
            OnUpdatePresentation(
                viewportWidth: (int)sizeDelta.x,
                viewportHeight: (int)sizeDelta.y,
                orientation: m_CurrentScreenOrientation,
                renderingMaterial: m_RawImage.material,
                image: out var texture,
                displayMatrix: out var displayMatrix);

            m_RawImage.texture = texture;
            m_RawImage.material.SetMatrix(k_DisplayMatrix, displayMatrix);

            if (m_RawImage.texture != null)
            {
                // Display some text information about each of the textures.
                var displayTexture = m_RawImage.texture as Texture2D;
                if (displayTexture != null)
                {
                    m_StringBuilder.Clear();
                    BuildTextureInfo(m_StringBuilder, "env", displayTexture);
                    LogText(m_StringBuilder.ToString());
                }
            }
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
        protected abstract void OnUpdatePresentation( int viewportWidth, int viewportHeight, ScreenOrientation orientation,
            Material renderingMaterial, out Texture image, out Matrix4x4 displayMatrix);


        private static void BuildTextureInfo(StringBuilder stringBuilder, string textureName, Texture2D texture)
        {
            stringBuilder.AppendLine($"texture : {textureName}");
            if (texture == null)
            {
                stringBuilder.AppendLine("   <null>");
            }
            else
            {
                stringBuilder.AppendLine($"   format : {texture.format}");
                stringBuilder.AppendLine($"   width  : {texture.width}");
                stringBuilder.AppendLine($"   height : {texture.height}");
                stringBuilder.AppendLine($"   mipmap : {texture.mipmapCount}");
            }
        }

        private void LogText(string text)
        {
            if (m_ImageInfo != null)
            {
                m_ImageInfo.text = text;
            }
            else
            {
                Debug.Log(text);
            }
        }

        private void UpdateRawImage()
        {
            Debug.Assert(m_RawImage != null, "no raw image");

            // The aspect ratio of the presentation in landscape orientation
            var aspect = Mathf.Max(m_camera.pixelWidth, m_camera.pixelHeight) /
                (float)Mathf.Min(m_camera.pixelWidth, m_camera.pixelHeight);

            // Determine the raw image rectSize preserving the texture aspect ratio, matching the screen orientation,
            // and keeping a minimum dimension size.
            float minDimension = 480.0f;
            float maxDimension = Mathf.Round(minDimension * aspect);
            Vector2 rectSize;
            switch (m_CurrentScreenOrientation)
            {
                case ScreenOrientation.LandscapeRight:
                case ScreenOrientation.LandscapeLeft:
                    rectSize = new Vector2(maxDimension, minDimension);
                    break;
                case ScreenOrientation.PortraitUpsideDown:
                case ScreenOrientation.Portrait:
                default:
                    rectSize = new Vector2(minDimension, maxDimension);
                    break;
            }

            // Update the raw image dimensions and the raw image material parameters.
            m_RawImage.rectTransform.sizeDelta = rectSize;
            m_RawImage.material = m_Material;
        }
    }
}
