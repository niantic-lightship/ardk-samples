using System;
using System.Collections;
using System.Text;
using UnityEngine.UI;

namespace UnityEngine.XR.ARFoundation.Samples
{
    /// <summary>
    /// This component overlays the environment depth texture to the camera image.
    /// </summary>
    public class FitDepthToImage : MonoBehaviour
    {
        /// <summary>
        /// Name of the display rotation matrix in the shader.
        /// </summary>
        private const string k_DisplayMatrix = "_DisplayMatrix";

        /// <summary>
        /// ID of the display rotation matrix in the shader.
        /// </summary>
        private static readonly int k_DisplayMatrixId = Shader.PropertyToID(k_DisplayMatrix);

        /// <summary>
        /// A string builder for construction of strings.
        /// </summary>
        private readonly StringBuilder m_StringBuilder = new();

        [SerializeField]
        [Tooltip("The ARCameraManager which will produce camera frame events.")]
        private ARCameraManager m_CameraManager;

        [SerializeField]
        [Tooltip("The AROcclusionManager which will produce depth textures.")]
        private AROcclusionManager m_OcclusionManager;

        [SerializeField]
        private DepthPicker m_DepthPicker;

        [SerializeField]
        private Material m_DepthMaterial;

        [SerializeField]
        private RawImage m_RawImage;

        [SerializeField]
        private Button m_BackButton;

        [SerializeField]
        private Button m_PlaceButton;

        [SerializeField]
        private Text m_ImageInfo;

        // Cached display matrix
        private Matrix4x4 m_DisplayMatrix = Matrix4x4.identity;

        // The display matrix coming from AR Foundation can vary per
        // platform unfortunately. We need to know what is the running
        // platform to apply the matrix properly in the shader.
        private const string k_AndroidPlatform = "ANDROID_PLATFORM";

        private void Awake()
        {
            Debug.Assert(m_RawImage != null, "no raw image");

#if UNITY_ANDROID
            m_DepthMaterial.EnableKeyword(k_AndroidPlatform);
#else
            m_DepthMaterial.DisableKeyword(k_AndroidPlatform);
#endif
            m_RawImage.material = m_DepthMaterial;
            m_RawImage.material.SetMatrix(k_DisplayMatrixId, m_DisplayMatrix);
        }

        private void OnEnable()
        {
            // Subscribe to the camera frame received event, and initialize the display rotation matrix.
            Debug.Assert(m_CameraManager != null, "no camera manager");
            m_CameraManager.frameReceived += OnCameraFrameEventReceived;
        }

        private void OnDisable()
        {
            // Unsubscribe to the camera frame received event
            if (m_CameraManager != null)
                m_CameraManager.frameReceived -= OnCameraFrameEventReceived;
        }

        private void Update()
        {
            // Grab the latest depth texture
            var environmentDepthTexture = m_OcclusionManager.environmentDepthTexture;
            if (environmentDepthTexture == null)
                return;

            // Display some text information about the texture
            m_StringBuilder.Clear();
            BuildTextureInfo(m_StringBuilder, "env", environmentDepthTexture);
            LogText(m_StringBuilder.ToString());

            // Assign the texture to display to the raw image.
            m_RawImage.texture = environmentDepthTexture;
            m_RawImage.material.SetMatrix(k_DisplayMatrixId, m_DisplayMatrix);
        }

        /// <summary>
        /// When the camera frame event is raised, capture the display matrix.
        /// </summary>
        private void OnCameraFrameEventReceived(ARCameraFrameEventArgs cameraFrameEventArgs)
        {
            if (cameraFrameEventArgs.displayMatrix.HasValue)
            {
                m_DisplayMatrix = cameraFrameEventArgs.displayMatrix.Value;
#if UNITY_ANDROID && !UNITY_EDITOR
                m_DisplayMatrix = m_DisplayMatrix.transpose;
#endif
            }
        }

        /// <summary>
        /// Invoked when the 'Place' button is pressed.
        /// </summary>
        public void PlaceMarker()
        {
            // Disable UI
            m_PlaceButton.interactable = false;
            m_BackButton.interactable = false;

            StartCoroutine(Perform(() =>
            {
                // Wait for the user to place the marker ...
                m_DepthPicker.PlaceMarker(onFinished: () =>
                {
                    // ... and then re-enable the UI
                    m_PlaceButton.interactable = true;
                    m_BackButton.interactable = true;
                });
            },
                // Need to make sure the button is released before placing the marker
                condition: () => Input.touchCount == 0 && !Input.GetMouseButton(0)));
        }

        /// <summary>
        /// Waits for the condition to be satisfied before invoking the specified action.
        /// </summary>
        /// <param name="what">Action to invoke.</param>
        /// <param name="condition">The condition to fulfill.</param>
        private IEnumerator Perform(Action what, Func<bool> condition)
        {
            if (condition != null)
            {
                while (!condition.Invoke())
                {
                    yield return null;
                }
            }

            what?.Invoke();
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
        /// Create log information about the given texture.
        /// </summary>
        /// <param name="stringBuilder">The string builder to which to append the texture information.</param>
        /// <param name="textureName">The semantic name of the texture for logging purposes.</param>
        /// <param name="texture">The texture for which to log information.</param>
        private void BuildTextureInfo(StringBuilder stringBuilder, string textureName, Texture2D texture)
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
    }
}
