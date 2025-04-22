// Copyright 2022-2025 Niantic.
using System;
using System.Collections;
using System.Collections.Generic;
using Niantic.Lightship.AR.Occlusion;
using Niantic.Lightship.AR.Semantics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class DepthOcclusionSample : MonoBehaviour
{
    [SerializeField] private AROcclusionManager _occlusionManager;
    [SerializeField] private ARSemanticSegmentationManager _segmentationManager;
    [SerializeField] private LightshipOcclusionExtension _occlusionExtension;
    [SerializeField] private SliderToggle _suppressionToggle;
    [SerializeField] private SliderToggle _stabilizationToggle;
    [SerializeField] private Text _loadingText;

    private bool _occlusionReady = false;
    private bool _semanticsReady = false;

    private void OnEnable()
    {
        _suppressionToggle.interactable = false;
        _stabilizationToggle.interactable = false;
        
        _occlusionManager.frameReceived += OnOcclusionReady;
        _segmentationManager.MetadataInitialized += OnSemanticsReady;
    }
    
    private void OnDisable()
    {
        _suppressionToggle.onValueChanged.RemoveListener(ToggleSuppression);
        _stabilizationToggle.onValueChanged.RemoveListener(ToggleStabilization);

        if (!_occlusionReady)
        {
            _occlusionManager.frameReceived -= OnOcclusionReady;
        }

        if (!_semanticsReady)
        {
            _segmentationManager.MetadataInitialized -= OnSemanticsReady;
        }
    }

    private void OnOcclusionReady(AROcclusionFrameEventArgs frameEventArgs)
    {
        _occlusionReady = true;
        _occlusionManager.frameReceived -= OnOcclusionReady;

        TryActivateUI();
    }
    
    private void OnSemanticsReady(ARSemanticSegmentationModelEventArgs modelEventArgs)
    {
        _semanticsReady = true;
        _segmentationManager.MetadataInitialized -= OnSemanticsReady;

        TryActivateUI();
    }

    private void TryActivateUI()
    {
        if (_occlusionReady && _semanticsReady)
        {
            _suppressionToggle.onValueChanged.AddListener(ToggleSuppression);
            _stabilizationToggle.onValueChanged.AddListener(ToggleStabilization);
        
            _suppressionToggle.interactable = true;
            _stabilizationToggle.interactable = true;
            
            _loadingText.gameObject.SetActive(false);
        }
    }
    
    private void ToggleSuppression(bool on)
    {
        _occlusionExtension.IsOcclusionSuppressionEnabled = on;
    }
    
    private void ToggleStabilization(bool on)
    {
        _occlusionExtension.IsOcclusionStabilizationEnabled = on;
    }
}
