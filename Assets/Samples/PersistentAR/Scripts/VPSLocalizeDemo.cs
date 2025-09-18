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
    private Text _meshDownloadStatusText;

    [SerializeField] private Image _downloadLoadingIcon;

    [SerializeField]
    private LocalizationGuidanceManager _localizationGuidanceManager;
    
    [SerializeField]
    private GameObject _requestVPSSettings;

    [SerializeField]
    private GameObject _meshSettings;
    
    [SerializeField]
    private Slider _meshTransparencySlider;

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

        _meshDownloadStatusText.text = "Not Started";
        // set the loading effect to false
        _downloadLoadingIcon.gameObject.SetActive(false);
        
        _meshTransparencySlider.onValueChanged.AddListener(OnMeshTransparencySliderValueChanged);

        _arLocationHolder = new GameObject("ARLocation");
        // The ARLocation will be enabled once the anchor starts tracking.
        _arLocationHolder.SetActive(false);
        _localizationStatusPanel.SetActive(false);
        // Show RequestVPSSettings and hide MeshSettings
        _requestVPSSettings.SetActive(true);
        _meshSettings.SetActive(false);
    }

    private void OnDestroy()
    {
        _vpsCoverageTargetListManager.OnWayspotDefaultAnchorButtonPressed -= OnLocationSelected;
        _arLocationManager.locationTrackingStateChanged -= OnLocationTrackingStateChanged;
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
        
        _meshTransparencySlider.onValueChanged.RemoveListener(OnMeshTransparencySliderValueChanged);
    }

    private void OnLocationSelected(VpsCoverageTargetListManager.WayspotSelectedArgs location)
    {
        // Hide RequestVPSSettings and show MeshSettings
        _requestVPSSettings.SetActive(false);
        _meshSettings.SetActive(true);
        
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

        // Start downloading the mesh for the location.
        _downloadMeshToggle.isOn = true;
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
             _localizationStatusText.text = "NOT TRACKING";

             _isTracking = false;
            // We de-activate the gameObject when we lose tracking.
            // ARLocationManager will not de-activate it
            args.ARLocation.gameObject.SetActive(false);
         }
    }
    
    private async Task DownloadAndPositionMeshAsync(ARLocation location)
    {
        // start loading effect
        _downloadLoadingIcon.gameObject.SetActive(true);

        _meshDownloadStatusText.text = "Downloading";
        _localizationStatusText.text = "DOWNLOADING MESH";
        
        var payload = location.Payload;
        var go = await _meshManager.GetLocationMeshForPayloadAsync(payload.ToBase64(), 0, false, true);
        // end loading effect
        _downloadLoadingIcon.gameObject.SetActive(false);
        
        // if donwload failed
        if (go == null)
        {
            _meshDownloadStatusText.text = "Download Failed";
        }
        else
        {
            _meshDownloadStatusText.text = "Download Succeeded";
            go.transform.SetParent(location.transform, false);
            _downloadedMesh = go;
        }
        
        _localizationStatusText.text = _isTracking ? "TRACKING" : "NOT TRACKING";
    }

    private void OnMeshTransparencySliderValueChanged(float value)
    {
        if (!_downloadedMesh) return;
        foreach (var mr in _downloadedMesh.GetComponentsInChildren<MeshRenderer>())
        {
            // Set the material to transparent mode (3 = Transparent mode)
            mr.material.SetFloat("_Mode", 3);

            // Get current color and modify its alpha
            Color color = mr.material.color;
            color.a = value;
            mr.material.color = color;

            // Make sure necessary rendering settings are enabled for transparency
            mr.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mr.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mr.material.SetInt("_ZWrite", 1);
            mr.material.DisableKeyword("_ALPHATEST_ON");
            mr.material.EnableKeyword("_ALPHABLEND_ON");
            mr.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mr.material.renderQueue = 3000;
        }
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
            _downloadLoadingIcon.gameObject.SetActive(false);
        }
    }
}
