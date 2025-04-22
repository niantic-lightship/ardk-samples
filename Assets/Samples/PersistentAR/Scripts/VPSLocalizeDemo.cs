// Copyright 2022-2025 Niantic.
using System;
using System.Linq;
using System.Threading.Tasks;

using Niantic.Lightship.AR.LocationAR;
using Niantic.Lightship.AR.PersistentAnchors;
using Niantic.Lightship.AR.Samples;
using Niantic.Lightship.AR.Subsystems;

using UnityEngine;
using UnityEngine.UI;

public class VPSLocalizeDemo : MonoBehaviour
{
    [Tooltip("VPS Coverage list manager")] [SerializeField]
    private VpsCoverageTargetListManager _vpsCoverageTargetListManager;
    [Tooltip("The location manager")] [SerializeField]
    private ARLocationManager _arLocationManager;

    [SerializeField]
    private LocationMeshManager _meshManager;

    [SerializeField]
    private Text _localizationStatusText;

    [SerializeField]
    private GameObject _localizationStatusPanel;

    [SerializeField]
    private GameObject _anchorMarkerPrefab;
    
    [SerializeField]
    private Toggle _downloadMeshToggle;

    [SerializeField]
    private LocalizationGuidanceManager _localizationGuidanceManager;

    //Holder object for the AR location payload.
    private GameObject _arLocationHolder;
    
    // Only attempt mesh download once per demo run. If it starts, it will not start again.
    private bool _meshDownloadStarted;
    private GameObject _downloadedMesh;
    private bool _isTracking;

    private void Start()
    {
        _arLocationManager.locationTrackingStateChanged += OnLocationTrackingStateChanged;
        _vpsCoverageTargetListManager.OnWayspotDefaultAnchorButtonPressed += OnLocationSelected;
        if (_downloadMeshToggle)
        {
            _downloadMeshToggle.onValueChanged.AddListener(OnDownloadMeshToggleValueChanged);
        }

        _arLocationHolder = new GameObject("ARLocation");
        // The ARLocation will be enabled once the anchor starts tracking.
        _arLocationHolder.SetActive(false);
        _localizationStatusPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        _vpsCoverageTargetListManager.OnWayspotDefaultAnchorButtonPressed -= OnLocationSelected;
        _localizationGuidanceManager.StopGuidance();
        if (_arLocationHolder != null)
        {
            Destroy(_arLocationHolder);
        }
        
        if (_downloadedMesh)
        {
            Destroy(_downloadedMesh);
        }

        if (_downloadMeshToggle)
        {
            _downloadMeshToggle.onValueChanged.RemoveListener(OnDownloadMeshToggleValueChanged);
        }
    }

    private void OnLocationSelected(VpsCoverageTargetListManager.WayspotSelectedArgs location)
    {
        if (String.IsNullOrEmpty(location.Payload))
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
        _arLocation.Payload = new ARPersistentAnchorPayload(location.Payload);
        _arLocationManager.SetARLocations(_arLocation);
        _arLocationManager.StartTracking();

        _vpsCoverageTargetListManager.gameObject.SetActive(false);
        _localizationStatusText.text = "NOT TRACKING";
        _localizationStatusPanel.SetActive(true);
        _localizationGuidanceManager.StartGuidance();
    }

    private void OnLocationTrackingStateChanged(ARLocationTrackedEventArgs args)
    {
         if (args.Tracking)
         {
            _localizationStatusText.text = "TRACKING";
            _isTracking = true;
            _localizationGuidanceManager.StopGuidance();
            if (!_meshDownloadStarted && _downloadMeshToggle.isOn)
            {
                _meshDownloadStarted = true;
                _ = DownloadAndPositionMeshAsync(location: args.ARLocation);
            }
         }
         else
         {
            if(_localizationStatusText != null){
                _localizationStatusText.text = "NOT TRACKING";
            }
            
            _isTracking = false;
            // We de-activate the gameObject when we lose tracking.
            // ARLocationManager will not de-activate it
            args.ARLocation.gameObject.SetActive(false);
         }
    }
    
    private async Task DownloadAndPositionMeshAsync(ARLocation location)
    {
        var payload = location.Payload;
        var go = await _meshManager.GetLocationMeshForPayloadAsync(payload.ToBase64());
        go.transform.SetParent(location.transform, false);
        _downloadedMesh = go;
    }

    private void OnDownloadMeshToggleValueChanged(bool newValue)
    {
        if (newValue && _isTracking && !_meshDownloadStarted)
        {
            _meshDownloadStarted = true;
            _ = DownloadAndPositionMeshAsync(_arLocationManager.ARLocations.First());
        }
        else if(_meshDownloadStarted && _downloadedMesh)
        {
            _downloadedMesh.SetActive(newValue);
        }
    }
}
