// Copyright 2022-2024 Niantic.
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class DepthDisplayDemo : MonoBehaviour
{
    public ARCameraManager _cameraManager;

    public AROcclusionManager _occlusionManager;

    public RawImage _rawImage;

    public Material _material;

    private Matrix4x4 _displayMat = Matrix4x4.identity;

    void OnEnable()
    {
        _cameraManager.frameReceived += OnCameraFrameUpdate;
    }

    private void OnDisable()
    {
        _cameraManager.frameReceived -= OnCameraFrameUpdate;
    }

    void Update()
    {
        if (!_occlusionManager.subsystem.running)
        {
            return;
        }

        //add our material to the raw image
        _rawImage.material = _material;

        //set our variables in our shader
        //NOTE: Updating the depth texture needs to happen in the Update() function
        _rawImage.material.SetTexture("_DepthTex", _occlusionManager.environmentDepthTexture);
        _rawImage.material.SetMatrix("_DisplayMat", _displayMat);
    }

    private void OnCameraFrameUpdate(ARCameraFrameEventArgs args)
    {
        if (!_occlusionManager.subsystem.running)
        {
            return;
        }

        //get the display matrix
        _displayMat = args.displayMatrix ?? Matrix4x4.identity;

        #if UNITY_ANDROID
            _displayMat = _displayMat.transpose;
            #if UNITY_EDITOR
                _displayMat = _displayMat.inverse;
            #endif
        #endif
    }
}