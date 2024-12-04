using System;
using System.Collections;
using System.IO;
using Niantic.Lightship.AR.Mapping;
using Niantic.Lightship.AR.PersistentAnchors;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// This class handles localising to a map
/// It loads the anchor from a file on device
/// An has helpers functions for placing things relative to the maps anchor
/// </summary>
public class Tracker : MonoBehaviour
{
    [SerializeField]
    private ARPersistentAnchorManager _persistentAnchorManager;

    [SerializeField]
    private ARDeviceMappingManager _deviceMappingManager;

    public ARPersistentAnchor Anchor {get => _anchor; }
    private ARPersistentAnchor _anchor;
    
    //subscribe to this to know when tracking has beed successful
    public Action<bool> _tracking;
    
    private void Start()
    {
        _persistentAnchorManager.DeviceMappingLocalizationEnabled = true;
        _persistentAnchorManager.CloudLocalizationEnabled = false;
        _persistentAnchorManager.ContinuousLocalizationEnabled = true;
        _persistentAnchorManager.TemporalFusionEnabled = true;
        _persistentAnchorManager.TransformUpdateSmoothingEnabled = true;
        _persistentAnchorManager.DeviceMappingLocalizationRequestIntervalSeconds = 0.1f;
        StartCoroutine(_persistentAnchorManager.RestartSubsystemAsyncCoroutine());
    }

    private void OnArPersistentAnchorStateChanged(ARPersistentAnchorStateChangedEventArgs args)
    {
        if (args.arPersistentAnchor.trackingState == TrackingState.Tracking)
        {
            _tracking?.Invoke(true);
        }
    }
    
    //tracking should remain on until you no 
    public void StartTracking()
    {
        _persistentAnchorManager.arPersistentAnchorStateChanged += OnArPersistentAnchorStateChanged;

        StartCoroutine(RestartTracking());
    }
    
    public void StopAndDestroyAnchor()
    {
        _persistentAnchorManager.enabled = false;
        if (_anchor)
        {
            _persistentAnchorManager.DestroyAnchor(_anchor);
        }
        
        _persistentAnchorManager.arPersistentAnchorStateChanged -= OnArPersistentAnchorStateChanged;
    }

    private IEnumerator RestartTracking()
    {
        if (_anchor)
        {
            _persistentAnchorManager.DestroyAnchor(_anchor);
        }
        _persistentAnchorManager.enabled = false;

        // start tracking after stop tracking needs "some" time in between...
        yield return null;

        _persistentAnchorManager.enabled = true;

        // Read a new device map from file
        var fileName =  OnDevicePersistence.k_mapFileName;
        var path = Path.Combine(Application.persistentDataPath, fileName);
        var serializedDeviceMap = File.ReadAllBytes(path);
        var deviceMap = ARDeviceMap.CreateFromSerializedData(serializedDeviceMap);
        
        // Set the device map to mapping manager
        _deviceMappingManager.SetDeviceMap(deviceMap);

        // Set up a new tracking with a new anchor
        _persistentAnchorManager.TryTrackAnchor(
            new ARPersistentAnchorPayload(deviceMap.GetAnchorPayload()),
            out _anchor);
    }


    //convert a world point to an anchor relative one.
    public Vector3 GetAnchorRelativePosition(Vector3 pos)
    {
        return _anchor.transform.InverseTransformPoint(pos);
    }

    //parent the game object under the anchor.
    public void AddObjectToAnchor(GameObject go)
    {
        go.transform.SetParent(_anchor.transform);
    }

}
