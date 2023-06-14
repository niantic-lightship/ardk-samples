using System.Text;
using Niantic.Lightship.AR.Utilities;
using UnityEngine.UI;

namespace UnityEngine.XR.ARFoundation.Samples
{
    public abstract class DisplayImage : MonoBehaviour
    {
        /// <summary>
        /// Name of the display rotation matrix in the shader.
        /// </summary>
        const string k_DisplayMatrixName = "_DisplayMatrix";

        /// <summary>
        /// ID of the display matrix in the shader.
        /// </summary>
        private readonly int k_DisplayMatrix = Shader.PropertyToID(k_DisplayMatrixName);

        /// <summary>
        /// A string builder for construction of strings.
        /// </summary>
        protected readonly StringBuilder m_StringBuilder = new();

        /// <summary>
        /// The current screen orientation remembered so that we are only updating the raw image layout when it changes.
        /// </summary>
        protected ScreenOrientation m_CurrentScreenOrientation;

        [SerializeField]
        [Tooltip("The ARCameraManager which will produce camera frame events.")]
        ARCameraManager m_CameraManager;

        [SerializeField]
        protected RawImage m_RawImage;

        [SerializeField]
        Material m_Material;

        [SerializeField]
        Text m_ImageInfo;

        // The rendering Unity camera
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

            OnUpdateImage(m_RawImage);

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

                // Calculate a transform to fit depth to the raw image
                var sizeDelta = m_RawImage.rectTransform.sizeDelta;
                var displayMatrix = _CameraMath.CalculateDisplayMatrix(
                    m_RawImage.texture.width,
                    m_RawImage.texture.height,
                    (int)sizeDelta.x,
                    (int)sizeDelta.y,
                    Screen.orientation,
                    layout: _CameraMath.MatrixLayout.RowMajor
                );

                // Assign the display matrix
                m_RawImage.material.SetMatrix(k_DisplayMatrix, displayMatrix);
            }
        }

        protected abstract void OnUpdateImage(RawImage image);

        /// <summary>
        /// Create log information about the given texture.
        /// </summary>
        /// <param name="stringBuilder">The string builder to which to append the texture information.</param>
        /// <param name="textureName">The semantic name of the texture for logging purposes.</param>
        /// <param name="texture">The texture for which to log information.</param>
        static protected void BuildTextureInfo(StringBuilder stringBuilder, string textureName, Texture2D texture)
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

        /// <summary>
        /// Log the given text to the screen if the image info UI is set. Otherwise, log the string to debug.
        /// </summary>
        /// <param name="text">The text string to log.</param>
        protected void LogText(string text)
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

        /// <summary>
        /// Update the raw image with the current configurations.
        /// </summary>
        protected void UpdateRawImage()
        {
            Debug.Assert(m_RawImage != null, "no raw image");

            // The aspect ratio of the presentation in landscape orientation
            var aspect = Mathf.Max(m_camera.pixelWidth, m_camera.pixelHeight) /
                (float)Mathf.Min(m_camera.pixelWidth, m_camera.pixelHeight);

            // Determine the raw image rectSize preserving the texture aspect ratio, matching the screen orientation,
            // and keeping a minimum dimension size.
            float minDimension = 360.0f;
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

