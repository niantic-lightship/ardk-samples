// Copyright 2023 Niantic, Inc. All Rights Reserved.
using System.Linq;
using Niantic.Lightship.AR.Semantics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARSubsystems;

namespace Niantic.Lightship.AR.Samples
{

    public sealed class DisplaySemanticsImage : DisplayImage
    {
        [SerializeField]
        [Tooltip("The ARSemanticSegmentationManager which will produce semantics textures.")]
        private ARSemanticSegmentationManager _semanticsManager;

        [SerializeField]
        private Dropdown _channelDropdown;

        private string _semanticChannelName = string.Empty;

        protected override void Awake()
        {
            base.Awake();
            Debug.Assert(_semanticsManager != null, "Missing semantic segmentation manager component.");
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _semanticsManager.MetadataInitialized += SemanticsManager_OnDataInitialized;
            _channelDropdown.onValueChanged.AddListener(ChannelDropdown_OnValueChanged);
        }

        private void OnDisable()
        {
            _semanticsManager.MetadataInitialized -= SemanticsManager_OnDataInitialized;
            _channelDropdown.onValueChanged.RemoveListener(ChannelDropdown_OnValueChanged);
        }

        protected override void OnUpdatePresentation(int viewportWidth, int viewportHeight,ScreenOrientation orientation,
            Material renderingMaterial, out Texture image, out Matrix4x4 displayMatrix)
        {
            // Use the XRCameraParams type to describe the viewport to fit the semantics image to
            var viewport = new XRCameraParams
            {
                screenWidth = viewportWidth, screenHeight = viewportHeight, screenOrientation = orientation
            };

            // Update the texture with the confidence values of the currently selected channel
            image = _semanticsManager.GetSemanticChannelTexture(_semanticChannelName, out displayMatrix, viewport);
            renderingMaterial.SetTexture("_SemanticTex", image);
        }

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

        private void ChannelDropdown_OnValueChanged(int val)
        {
            // Update the display channel from the dropdown value.
            _semanticChannelName = _channelDropdown.options[val].text;
        }
    }
}
