// Copyright 2022-2025 Niantic.

using System.Collections;
using System.Collections.Generic;

using Niantic.Lightship.AR.WorldPositioning;
using Niantic.Lightship.AR.XRSubsystems;

using UnityEngine;

public class StatusHelper : MonoBehaviour
{
    [Tooltip("The World Space Manager for the scene")] [SerializeField]
    private ARWorldPositioningManager _wpsManager;
    
    [Tooltip("Object to show when the status is Available")] [SerializeField]
    private GameObject _availableObject;

    [Tooltip("Object to show when the status is NoGnss")] [SerializeField]
    private GameObject _noGnssObject;

    [Tooltip("Object to show when the status is TrackingFailed")] [SerializeField]
    private GameObject _trackingFailedObject;

    [Tooltip("Object to show when the status is NoHeading")] [SerializeField]
    private GameObject _noHeadingObject;

    [Tooltip("Object to show when the status is Initializing")] [SerializeField]
    private GameObject _initializingObject;

    [Tooltip("Object to show when the status is SubsystemNotRunning")] [SerializeField]
    private GameObject _subsystemNotRunningObject;

    [Tooltip("Object to show when the status is NoGnss and the location permission has not been granted")] [SerializeField]
    private GameObject _noLocationPermissionObject;

    // Start is called before the first frame update
    void Start()
    {
        if(_initializingObject != null)
            _initializingObject.SetActive(true);
    }

    private GameObject objectForStatus(WorldPositioningStatus status)
    {
        switch(status)
        {
            case WorldPositioningStatus.Available:
                return _availableObject;
            case WorldPositioningStatus.NoGnss:
            {
                if (!UnityEngine.Input.location.isEnabledByUser)
                {
                    return _noLocationPermissionObject;
                }
                else
                {
                    return _noGnssObject;
                }
            }
            case WorldPositioningStatus.TrackingFailed:
                return _trackingFailedObject;
            case WorldPositioningStatus.NoHeading:
                return _noHeadingObject;
            case WorldPositioningStatus.Initializing:
                return _initializingObject;
            case WorldPositioningStatus.SubsystemNotRunning:
                return _subsystemNotRunningObject;
        }

        return null;
    }

    // Update is called once per frame
    void Update()
    {
        GameObject activeObject = objectForStatus(_wpsManager.Status);

        List<GameObject> allObjects = new(){_availableObject, _noGnssObject, _trackingFailedObject, _noHeadingObject, _initializingObject, _subsystemNotRunningObject, _noLocationPermissionObject};

        foreach(GameObject gameObject in allObjects)
        {
            if(gameObject != null)
            {
                if(gameObject == activeObject)
                {
                    gameObject.SetActive(true);
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
