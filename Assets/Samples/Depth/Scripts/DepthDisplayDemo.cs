// Copyright 2022-2025 Niantic.

using Niantic.Lightship.AR.Occlusion;
using Niantic.Lightship.AR.Utilities;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class DepthDisplayDemo : MonoBehaviour
{
    [SerializeField]
    private AROcclusionManager _occlusionManager;

    [SerializeField]
    private LightshipOcclusionExtension _occlusionExtension;
    
    [SerializeField]
    private RawImage _rawImage;

    [SerializeField]
    private Material _material;

    private bool _displayInterpolatedDepth = true;

    private void Start()
    {
        // Assign the material to the raw image
        _rawImage.material = _material;
    }

    private void Update()
    {
        if (!_occlusionManager.subsystem.running)
        {
            return;
        }

        if (!_displayInterpolatedDepth)
        {
            DisplayDepth();
        }
        else
        {
            DisplayInterpolatedDepth();
        }
    }

    /// <summary>
    /// In this method we show how to correctly display the depth image on screen.
    /// </summary>
    private void DisplayDepth()
    {
        // In this example, we acquire the depth image from AR Foundation's occlusion
        // manager. This image has the same aspect ratio as the AR background image.
        var texture = _occlusionManager.environmentDepthTexture;
        
        // We use Lightship's math library to calculate a display matrix that fits
        // the depth image to the screen.
        var displayMatrix = CameraMath.CalculateDisplayMatrix
        (
            texture.width,
            texture.height,
            Screen.width,
            Screen.height,
            XRDisplayContext.GetScreenOrientation()
        );
        
        // Update the material
        _rawImage.material.SetTexture("_DepthTex", texture);
        _rawImage.material.SetMatrix("_DepthTransform", displayMatrix);
    }
    
    /// <summary>
    /// In this method we show how to display interpolated depth on screen using Lightship Occlusion Extension.
    /// </summary>
    private void DisplayInterpolatedDepth()
    {
        // In this example, we acquire the depth image from Lightship's AR occlusion
        // extension. The aspect ratio of this image is not guaranteed to match the
        // AR background image.
        var texture = _occlusionExtension.DepthTexture;
        
        // Besides the depth texture, the occlusion extension also provides an appropriate
        // transformation matrix to display depth on screen. This matrix is different from
        // a traditional display matrix, because it also contains warping to compensate for
        // missing frames.
        var displayMatrix = _occlusionExtension.DepthTransform;
        
        // Update the material
        _rawImage.material.SetTexture("_DepthTex", texture);
        _rawImage.material.SetMatrix("_DepthTransform", displayMatrix);
    }

    public void ToggleInterpolation()
    {
        _displayInterpolatedDepth = !_displayInterpolatedDepth;
    }
}