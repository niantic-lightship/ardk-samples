// Copyright 2022-2025 Niantic.
using Niantic.Lightship.AR.PersistentAnchors;
using Niantic.Lightship.AR.Subsystems;

using Unity.XR.CoreUtils;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class VPSDeviceDebugText : MonoBehaviour
{
    [SerializeField]
    private XROrigin _xrOrigin;

    [SerializeField]
    private ARPersistentAnchorManager _arPersistentAnchorManager;

    [SerializeField]
    private Text _trackingStateText;

    [SerializeField]
    private Text _devicePoseText;

    [SerializeField]
    private Text _anchorPoseText;

    private ARPersistentAnchor _arPersistentAnchor;

    private void OnEnable()
    {
        _arPersistentAnchorManager.arPersistentAnchorStateChanged += HandleARPersistentAnchorStateChanged;
    }

    // Start is called before the first frame update
    void Start()
    {
        _trackingStateText.text = "Waiting for AR Session state";
        if (_arPersistentAnchorManager == null)
        {
            _arPersistentAnchorManager = FindFirstObjectByType<ARPersistentAnchorManager>();
            if (_arPersistentAnchorManager == null)
            {
                _anchorPoseText.text = "Could not find ARPersistentAnchorManager";
            }
            else
            {
                _anchorPoseText.text = "Waiting for anchor creation";
            }
        }

        if (_xrOrigin == null)
        {
            _xrOrigin = FindFirstObjectByType<XROrigin>();
            if (_xrOrigin == null)
            {
                if (_xrOrigin == null)
                {
                    _devicePoseText.text = "Could not find XROrigin";
                }
                else
                {
                    _devicePoseText.text = "Waiting for device pose";
                }
            }
        }
    }

    void Update()
    {
        // Update pose every 4 frames
        if (Time.frameCount % 4 == 0)
        {
            if (_xrOrigin && _xrOrigin.Camera && _devicePoseText)
            {
                _devicePoseText.text =
                    $"Camera position is {_xrOrigin.Camera.gameObject.transform.position}";
            }

            if (_arPersistentAnchor && _anchorPoseText)
            {
                _anchorPoseText.text =
                    $"Anchor position is {_arPersistentAnchor.transform.position} " +
                    $"with rotation {_arPersistentAnchor.transform.rotation}";
            }
        }
    }

    private void OnDisable()
    {
        _arPersistentAnchorManager.arPersistentAnchorStateChanged -= HandleARPersistentAnchorStateChanged;
    }

    private void HandleARPersistentAnchorStateChanged(ARPersistentAnchorStateChangedEventArgs args)
    {
        if (args.arPersistentAnchor.trackingState == TrackingState.Tracking)
        {
            _arPersistentAnchor = args.arPersistentAnchor;
            var stateText = $"Tracking state is {args.arPersistentAnchor.trackingState:G} with reason {ARSession.notTrackingReason:G}";
            _trackingStateText.text = stateText;
        }
    }
}
