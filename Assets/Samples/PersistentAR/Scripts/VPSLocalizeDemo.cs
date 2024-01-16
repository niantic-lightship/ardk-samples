// Copyright 2022-2024 Niantic.
using System;
using Niantic.Lightship.AR.LocationAR;
using Niantic.Lightship.AR.PersistentAnchors;
using UnityEngine;
using UnityEngine.UI;

public class VPSLocalizeDemo : MonoBehaviour
{
    [Tooltip("VPS Coverage list manager")] [SerializeField]
    private VpsCoverageTargetListManager _vpsCoverageTargetListManager;
    [Tooltip("The location manager")] [SerializeField]
    private ARLocationManager _arLocationManager;

    [SerializeField]
    private Text _localizationStatusText;

    [SerializeField]
    private GameObject _localizationStatusPanel;

    [SerializeField]
    private GameObject _anchorMarkerPrefab;

    //Holder object for the AR location payload.
    private GameObject _arLocationHolder;
    private void Start()
    {
        _arLocationManager.locationTrackingStateChanged += OnLocationTrackingStateChanged;
        _vpsCoverageTargetListManager.OnWayspotDefaultAnchorButtonPressed += OnLocationSelected;

        _arLocationHolder = new GameObject("ARLocation");
        // The ARLocation will be enabled once the anchor starts tracking.
        _arLocationHolder.SetActive(false);
        _localizationStatusPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        _vpsCoverageTargetListManager.OnWayspotDefaultAnchorButtonPressed -= OnLocationSelected;
        if (_arLocationHolder != null)
        {
            Destroy(_arLocationHolder);
        }
    }

    private void OnLocationSelected(String defaultPayloadToSet)
    {
        if (String.IsNullOrEmpty(defaultPayloadToSet))
        {
            Debug.LogWarning("The selected location does not have a default anchor");
            return;
        }
        // ARLocationManager must be a component of XR Origin and the ARLocation component must be in a child
        // of it, and so here we set it up that way.
        var _arLocation = _arLocationHolder.AddComponent<ARLocation>();
        _arLocationHolder.transform.SetParent(_arLocationManager.transform);

        if (_anchorMarkerPrefab)
        {
            // Add the anchor marker as a child of the ARLocation. This marker will be placed at
            // the origin of the location's coordinate system.
            var anchorMarker = Instantiate(_anchorMarkerPrefab);
            anchorMarker.transform.SetParent(_arLocationHolder.transform);
        }

        //Once it is setup, we assign the payload and start tracking the ARLocation.
        _arLocation.Payload = new ARPersistentAnchorPayload(defaultPayloadToSet);
        _arLocationManager.SetARLocations(_arLocation);
        _arLocationManager.StartTracking();

        _vpsCoverageTargetListManager.gameObject.SetActive(false);
        _localizationStatusText.text = "NOT TRACKING";
        _localizationStatusPanel.SetActive(true);
    }

    private void OnLocationTrackingStateChanged(ARLocationTrackedEventArgs args)
    {
         if (args.Tracking)
         {
            _localizationStatusText.text = "TRACKING";
         }
         else
         {
            if(_localizationStatusText != null){
                _localizationStatusText.text = "NOT TRACKING";
            }
            // We de-activate the gameObject when we lose tracking.
            // ARLocationManager will not de-activate it
            args.ARLocation.gameObject.SetActive(false);
         }
    }
}
