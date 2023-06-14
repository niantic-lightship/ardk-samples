using System.Linq;
using Niantic.Lightship.AR.ARFoundation;
using Niantic.Lightship.AR.Utilities;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation.Samples;

namespace Niantic.Lightship.AR.Samples
{
    /// <summary>
    /// This component displays an overlay of semantic classification data.
    /// </summary>
    public sealed class DisplaySemanticsImage : DisplayImage
    {
        /// <summary>
        /// Name of the display rotation matrix in the shader.
        /// </summary>
        const string k_SamplerMatrixName = "_SamplerMatrix";

        /// <summary>
        /// ID of the display matrix in the shader.
        /// </summary>
        private readonly int k_SamplerMatrix = Shader.PropertyToID(k_SamplerMatrixName);

        [SerializeField]
        [Tooltip("The ARSemanticSegmentationManager which will produce semantics textures.")]
        ARSemanticSegmentationManager _semanticsManager;

        [SerializeField]
        Dropdown _channelDropdown;

        private string _semanticChannelName;

        protected override void Awake()
        {
            base.Awake();

            Debug.Assert(_semanticsManager != null, "No semantic segmentation manager");

            // Initialize the channel names in the dropdown menu.
            var channelNames = _semanticsManager.SemanticChannelNames;
            _channelDropdown.AddOptions(channelNames);

            // Display artificial ground by default.
            _semanticChannelName = channelNames[3];
            var dropdownList = _channelDropdown.options.Select(option => option.text).ToList();
            _channelDropdown.value = dropdownList.IndexOf(_semanticChannelName);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            _channelDropdown.onValueChanged.AddListener(delegate
            {
                OnSemanticChannelDropdownValueChanged(_channelDropdown);
            });
        }

        private void OnDisable()
        {
            _channelDropdown.onValueChanged.RemoveListener(delegate
            {
                OnSemanticChannelDropdownValueChanged(_channelDropdown);
            });
        }

        protected override void Update()
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
                var samplerMatrix = _CameraMath.CalculateDisplayMatrix(
                    m_RawImage.texture.width,
                    m_RawImage.texture.height,
                    (int)sizeDelta.x,
                    (int)sizeDelta.y,
                    Screen.orientation,
                    layout: _CameraMath.MatrixLayout.RowMajor
                );

                // Assign the sampler matrix (warp matrix * display matrix)
                m_RawImage.material.SetMatrix(k_SamplerMatrix, samplerMatrix);
            }
        }

        protected override void OnUpdateImage(RawImage image)
        {
            image.texture = _semanticsManager.GetSemanticChannelTexture(_semanticChannelName, out var samplerMatrix);
        }

        /// <summary>
        /// Callback when the semantic channel dropdown UI has a value change.
        /// </summary>
        /// <param name="channelDropdown">The dropdown UI that changed.</param>
        void OnSemanticChannelDropdownValueChanged(Dropdown channelDropdown)
        {
            // Update the display channel from the dropdown value.
            _semanticChannelName = channelDropdown.options[channelDropdown.value].text;
        }
    }
}
