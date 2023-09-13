using System.Linq;
using Niantic.Lightship.AR.ARFoundation;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation.Samples;
using UnityEngine.XR.ARSubsystems;

namespace Niantic.Lightship.AR.Samples
{
    /// <summary>
    /// This component displays an overlay of semantic classification data.
    /// </summary>
    public sealed class DisplaySemanticsImage : DisplayImage
    {
        [SerializeField]
        [Tooltip("The ARSemanticSegmentationManager which will produce semantics textures.")]
        private ARSemanticSegmentationManager _semanticsManager;

        [SerializeField]
        private Dropdown _channelDropdown;

        // The name of the actively selected semantic channel
        private string _semanticChannelName = string.Empty;

        // The texture to fill with confidence values of the selected channel
        private Texture _texture;

        protected override void Awake()
        {
            base.Awake();
            Debug.Assert(_semanticsManager != null, "Missing semantic segmentation manager component.");
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _semanticsManager.DataInitialized += SemanticsManager_OnDataInitialized;
            _channelDropdown.onValueChanged.AddListener(ChannelDropdown_OnValueChanged);
        }

        private void OnDisable()
        {
            _semanticsManager.DataInitialized -= SemanticsManager_OnDataInitialized;
            _channelDropdown.onValueChanged.RemoveListener(ChannelDropdown_OnValueChanged);
        }

        private void OnDestroy()
        {
            if (_texture != null)
            {
                Destroy(_texture);
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
        protected override void OnUpdatePresentation(int viewportWidth, int viewportHeight,ScreenOrientation orientation,
            Material renderingMaterial, out Texture image, out Matrix4x4 displayMatrix)
        {
            // Use the XRCameraParams type to describe the viewport to fit the semantics image to
            var viewport = new XRCameraParams
            {
                screenWidth = viewportWidth, screenHeight = viewportHeight, screenOrientation = orientation
            };

            // Update the texture with the confidence values of the currently selected channel
            _semanticsManager.GetSemanticChannelTexture(_semanticChannelName, viewport, ref _texture, out displayMatrix);

            // Forward the image to display
            image = _texture;
        }

        /// <summary>
        /// Invoked when the semantic segmentation model is downloaded and ready for use.
        /// </summary>
        private void SemanticsManager_OnDataInitialized(ARSemanticSegmentationModelEventArgs args)
        {
            // Initialize the channel names in the dropdown menu.
            var channelNames = _semanticsManager.ChannelNames;
            _channelDropdown.AddOptions(channelNames.ToList());

            // Display artificial ground by default.
            _semanticChannelName = channelNames[3];
            var dropdownList = _channelDropdown.options.Select(option => option.text).ToList();
            _channelDropdown.value = dropdownList.IndexOf(_semanticChannelName);
        }

        /// <summary>
        /// Callback when the semantic channel dropdown UI has a value change.
        /// </summary>
        private void ChannelDropdown_OnValueChanged(int val)
        {
            // Update the display channel from the dropdown value.
            _semanticChannelName = _channelDropdown.options[val].text;
        }
    }
}
