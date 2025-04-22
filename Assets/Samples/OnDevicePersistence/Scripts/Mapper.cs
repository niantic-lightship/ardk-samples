// Copyright 2022-2025 Niantic.
using System;
using System.Collections;
using System.IO;
using Niantic.Lightship.AR.Mapping;
using Niantic.Lightship.AR.MapStorageAccess;
using UnityEngine;

/// <summary>
/// This class manages creating local maps that are stored to a file on the device
/// </summary>
public class Mapper : MonoBehaviour
{
    [SerializeField]
    private ARDeviceMappingManager _deviceMappingManager;

    //subscribe to this to know mapping has completed
    public Action<bool> _onMappingComplete;

    //if we want the map to be written to the device or not.
    public bool _saveToFile = true;
    
    void Start()
    {
        //set up on device mapping
        _deviceMappingManager.MappingSplitterMaxDistanceMeters = 10.0f;
        _deviceMappingManager.MappingSplitterMaxDurationSeconds = 1000.0f;
        _deviceMappingManager.DeviceMapAccessController.OutputEdgeType = OutputEdgeType.All;
        
        //update manager to use new settings.
        StartCoroutine(_deviceMappingManager.RestartModuleAsyncCoroutine());
    }

    private Coroutine currentCo;
    private bool _mappingInProgress = false;
    public void RunMappingFor(float seconds)
    {
        _deviceMappingManager.DeviceMapFinalized += OnDeviceMapFinalized;
        currentCo = StartCoroutine(RunMapping(seconds));
    }

    public ARDeviceMap GetMap()
    {
        return _deviceMappingManager.ARDeviceMap;
    }

    private void OnDestroy()
    {
        _deviceMappingManager.DeviceMapFinalized -= OnDeviceMapFinalized;
    }
    
    //scanning is just on a timer to make some of the UX eaiser
    //you can easily modify the code have a start and stop button if you prefer
    private IEnumerator RunMapping(float seconds)
    {
        // Reset the ARDeviceMappingManager
        _deviceMappingManager.enabled = false;
        yield return null;
        _deviceMappingManager.enabled = true;
        yield return null;
        //start mapping
        _mappingInProgress = true;
        _deviceMappingManager.SetDeviceMap(new ARDeviceMap());
        _deviceMappingManager.StartMapping();
        
        //end mapping after a few seconds
        yield return new WaitForSeconds(seconds);
        _deviceMappingManager.StopMapping();
        _mappingInProgress = false;
    }

    //called if you hit exit while scanning is happening.
    public void StopMapping()
    {
        if (_mappingInProgress)
        {
            StopCoroutine(currentCo);
            _deviceMappingManager.DeviceMapFinalized -= OnDeviceMapFinalized;
            _deviceMappingManager.StopMapping();
            _mappingInProgress = false;
        }
    }

    
    public void ClearAllState()
    {
        StopMapping();
        _deviceMappingManager.enabled = false;
        _deviceMappingManager.DeviceMapAccessController.ClearDeviceMap();
    }

    private void OnDeviceMapFinalized(ARDeviceMap map)
    {
        _deviceMappingManager.DeviceMapFinalized -= OnDeviceMapFinalized;
        
        bool success = false;
        
        //if a map was created save it to a file
        if (map.HasValidMap())
        {
            success = true;
            if (_saveToFile == true)
            {
                // map update. save as a new map to the file system
                var fileName = OnDevicePersistence.k_mapFileName;
                var serializedDeviceMap = map.Serialize();
                var path = Path.Combine(Application.persistentDataPath, fileName);
                File.WriteAllBytes(path, serializedDeviceMap);
               
            }
        }
        _onMappingComplete?.Invoke(success);
    }
}
