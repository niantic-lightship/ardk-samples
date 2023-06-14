using System;
using UnityEngine;
using UnityEngine.UI;
using Niantic.Lightship.AR.Subsystems;
using UnityEngine.XR.ARSubsystems;

public class VPSLocalizeDemo : MonoBehaviour
{
    [SerializeField]
    private VpsCoverageTargetListManager _vpsCoverageTargetListManager;

    [SerializeField]
    private ARPersistentAnchorManager _persistentAnchorManager;

    [SerializeField]
    private Text _debugStatusText;

    private ARPersistentAnchor _arPersistentAnchor;

    void Start()
    {
        _persistentAnchorManager.arPersistentAnchorStateChanged += HandleLocationTrackingStateChanged;
        _vpsCoverageTargetListManager.OnWayspotDefaultAnchorButtonPressed += OnWayspotSelected;  
        
    }

    private void OnDestroy()
    {
        _vpsCoverageTargetListManager.OnWayspotDefaultAnchorButtonPressed -= OnWayspotSelected;
    }

    private void OnWayspotSelected(String defaultPayloadToSet)
    {
        if (String.IsNullOrEmpty(defaultPayloadToSet))
        {
            Debug.LogWarning("The selected wayspot does not have a default anchor");
            return;
        }
        var payload = new ARPersistentAnchorPayload(defaultPayloadToSet);
        _persistentAnchorManager.TryTrackAnchor(payload, out _arPersistentAnchor);
        _vpsCoverageTargetListManager.gameObject.SetActive(false);
        _debugStatusText.text = "Wayspot selected";
    }

    private void HandleLocationTrackingStateChanged(ARPersistentAnchorStateChangedEventArgs args)
    {
         if (_arPersistentAnchor.trackingState == TrackingState.Tracking)
        {
            Debug.Log("VPS localized.");
            _debugStatusText.text = "localized";
        }
        else
        {
            Debug.Log($"ARLocation not tracking?");
        }
    }
}
