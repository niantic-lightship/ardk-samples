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

        /// <summary>
        /// Create log information about the given texture.
        /// </summary>
        /// <param name="stringBuilder">The string builder to which to append the texture information.</param>
        /// <param name="textureName">The semantic name of the texture for logging purposes.</param>
        /// <param name="texture">The texture for which to log information.</param>
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

        /// <summary>
        /// Log the given text to the screen if the image info UI is set. Otherwise, log the string to debug.
        /// </summary>
        /// <param name="text">The text string to log.</param>
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

        /// <summary>
        /// Update the raw image with the current configurations.
        /// </summary>
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

