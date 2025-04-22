// Copyright 2022-2025 Niantic.
using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Niantic.Lightship.AR.Utilities;
using Input = UnityEngine.Input;

public class DepthPicker : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The AROcclusionManager which will produce depth textures.")]
    private AROcclusionManager m_OcclusionManager;

    [SerializeField]
    [Tooltip("The ARCameraManager which will produce camera frame events.")]
    private ARCameraManager m_CameraManager;

    [SerializeField]
    private GameObject m_Marker;

    // The rendering camera
    private Camera m_Camera;

    // Cached depth image
    private XRCpuImage? m_DepthImage;
    private Matrix4x4 m_DisplayMatrix;

    // State variable to indicate whether screen depth should be actively sampled
    private bool m_IsPlacing;
    private Action m_OnFinishedPlacing;

    private void Awake()
    {
        m_Camera = m_CameraManager.GetComponent<Camera>();
    }

    private void OnEnable()
    {
        m_CameraManager.frameReceived += OnCameraFrameEventReceived;
    }

    private void OnDisable()
    {
        m_CameraManager.frameReceived -= OnCameraFrameEventReceived;
    }

    private void OnCameraFrameEventReceived(ARCameraFrameEventArgs args)
    {
        // Cache the screen to image transform
        if (args.displayMatrix.HasValue)
        {
#if UNITY_IOS
            m_DisplayMatrix = args.displayMatrix.Value.transpose;
#else
            m_DisplayMatrix = args.displayMatrix.Value;
#endif
        }
    }

    private void Update()
    {
        if (!m_IsPlacing || (Input.touchCount <= 0 && !Input.GetMouseButton(0)))
        {
            return;
        }

        // Update the image
        if (m_OcclusionManager.TryAcquireEnvironmentDepthCpuImage(out var image))
        {
            m_DepthImage = image;
        }
        else
        {
            return;
        }

#if UNITY_EDITOR
        var screenPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
#else
        var screenPosition = Input.GetTouch(0).position;
#endif
        if (m_DepthImage.HasValue && m_DepthImage.Value.valid)
        {
            // Sample eye depth
            var uv = new Vector2(screenPosition.x / Screen.width, screenPosition.y / Screen.height);
            var eyeDepth = m_DepthImage.Value.Sample<float>(uv, m_DisplayMatrix);

            // Get world position
            var worldPosition =
                m_Camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, eyeDepth));

            // Update picker position
            m_Marker.transform.position = worldPosition;

            // Finished placing
            m_IsPlacing = false;

            // Performs additional actions
            m_OnFinishedPlacing?.Invoke();
            m_OnFinishedPlacing = null;
        }
    }

    public void PlaceMarker(Action onFinished = null)
    {
        m_IsPlacing = true;
        m_OnFinishedPlacing = onFinished;
    }
}
