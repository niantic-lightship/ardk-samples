// Copyright 2022-2025 Niantic.
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
    private ARPersistentAnchor _anchor;


    //do you want to load from a file or not.
    public bool _loadFromFile = true;
    
    //subscribe to this to know when tracking has beed successful
    public Action<bool> _tracking;
    
    //map data
    private ARDeviceMap _deviceMap;
    
    //adding a handle in case we try to add items to the scene before localiseation is finished
    //this could happen while using the data store.
    GameObject _tempAnchor;
    public Transform Anchor
    {
        get
        {
            //if we are localised return the anchor
            if(_anchor)
                return _anchor.transform;
            else
            {
                //if we are not yet localised return a temp root.
                if(!_tempAnchor)
                {
                    _tempAnchor = new GameObject("TempAnchor");
                }
            
                return _tempAnchor.transform;
            }
        }
    }
    
    private void Update()
    {
        //cleans up any items that were added before we localised by reparenting them to the proper anchor
        if (_anchor && _anchor.trackingState == TrackingState.Tracking)
        {
            if (_tempAnchor)
            {
                //move them
                for (int i = 0; i < _tempAnchor.transform.childCount; i++)
                {
                    _tempAnchor.transform.GetChild(i).SetParent(_anchor.transform, false);
                }
            }
        }
    }
    
    
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

        if (_loadFromFile)
        {
            // Read a new device map from file
            var fileName =  OnDevicePersistence.k_mapFileName;
            var path = Path.Combine(Application.persistentDataPath, fileName);
            var serializedDeviceMap = File.ReadAllBytes(path);
            _deviceMap = ARDeviceMap.CreateFromSerializedData(serializedDeviceMap);
        }

        StartCoroutine(RestartTrackingDataStore());
    }
    
    public void StopAndDestroyAnchor()
    {
        _persistentAnchorManager.enabled = false;
        if (_anchor)
        {
            _persistentAnchorManager.DestroyAnchor(_anchor);
        }

        _deviceMap = null;
        _persistentAnchorManager.arPersistentAnchorStateChanged -= OnArPersistentAnchorStateChanged;
    }

    public void ClearAllState()
    {
        StopAndDestroyAnchor();
        _deviceMappingManager.DeviceMapAccessController.ClearDeviceMap();
    }
    
    public void LoadMap(byte [] serializedDeviceMap)
    {
        _deviceMap = ARDeviceMap.CreateFromSerializedData(serializedDeviceMap);
    }

    private IEnumerator RestartTrackingDataStore()
    {
        //this needs to be set!
        while (_deviceMap == null)
            yield return new WaitForSeconds(1);

        _persistentAnchorManager.enabled = false;

        // start tracking after stop tracking needs "some" time in between...
        yield return null;

        _persistentAnchorManager.enabled = true;
        
        // Set the device map to mapping manager
        _deviceMappingManager.SetDeviceMap(_deviceMap);

        // Set up a new tracking with a new anchor
        _persistentAnchorManager.TryTrackAnchor(
            new ARPersistentAnchorPayload(_deviceMap.GetAnchorPayload()),
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
        go.transform.SetParent(Anchor.transform);
    }

}
