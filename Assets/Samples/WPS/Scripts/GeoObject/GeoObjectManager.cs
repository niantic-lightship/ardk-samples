using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Niantic.Lightship.AR.VpsCoverage;
using Niantic.Lightship.AR.WorldPositioning;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Pool;
using Input = Niantic.Lightship.AR.Input;

public class GeoObjectPair
{
    public WpsGeoObject WpsObject { get; }
    public GpsGeoObject GpsObject { get; }

    public GeoObjectPair(WpsGeoObject wpsObject, GpsGeoObject gpsObject)
    {
        WpsObject = wpsObject;
        GpsObject = gpsObject;
    }
}

public class GeoObjectManager : MonoBehaviour
{
    [SerializeField]
    private Camera _trackingCamera;
    
    [SerializeField]
    private XROrigin _xrOrigin;

    [SerializeField]
    private ARWorldPositioningObjectHelper _worldPositioningObjectHelper;
    
    [SerializeField]
    private ARWorldPositioningManager _worldPositioningManager;
    
    [SerializeField]
    private CoverageClientManager _coverageClientManager;
    
    [SerializeField]
    private Text _requestStatusText;

    [Header("Compass UI")]
    [SerializeField]
    private Button _compassWorldPose;
    
    [SerializeField]
    private Button _compassMagneticHeading;
    
    [Header("GeoObject Management")]
    [SerializeField]
    private WpsGeoObject _wpsGeoObjectPrefab;
    
    [SerializeField]
    private GpsGeoObject _gpsGeoObjectPrefab;
    
    // event handlers
    public event Action<GeoObjectPair> OnGeoObjectAdded;
    public event Action<GeoObjectPair> OnGeoObjectUpdated;
    public event Action<GeoObjectPair> OnGeoObjectRemoved;
    
    [Header("GeoObjectManager Parameters")]
    [SerializeField]
    // milliseconds
    private int _queryCoverageApiInterval = 10000;

    [SerializeField]
    private int _maxGeoObjectCount = 10;

    private IObjectPool<WpsGeoObject> _wpsGeoObjectPool;
    private IObjectPool<GpsGeoObject> _gpsGeoObjectPool;
    private readonly Dictionary<string, GeoObjectPair> _managedWpsGeoObjectPairs = new Dictionary<string, GeoObjectPair>();
    
    private GameObject _wpsGeoObjectParent;
    private GameObject _gpsGeoObjectParent;
    
    private CancellationTokenSource _cancellationTokenSource;
    
    private float _initialHeading;
    private float _initialYaw;
    
    private void Awake()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        
        if (!_trackingCamera) _trackingCamera = Camera.main;
        if (!_worldPositioningObjectHelper)
        {
            Debug.LogError("ARWorldPositioningObjectHelper is not assigned in the inspector.");
        }
        
        // Initialize ObjectPool
        _wpsGeoObjectPool = new UnityEngine.Pool.ObjectPool<WpsGeoObject>(
            createFunc: () => Instantiate(_wpsGeoObjectPrefab),
            actionOnGet: obj => obj.gameObject.SetActive(true),
            actionOnRelease: obj => obj.gameObject.SetActive(false),
            actionOnDestroy: obj => Destroy(obj.gameObject),
            collectionCheck: true,
            defaultCapacity: 10,
            maxSize: _maxGeoObjectCount > 0 ? _maxGeoObjectCount : 100
        );
        
        _gpsGeoObjectPool = new UnityEngine.Pool.ObjectPool<GpsGeoObject>(
            createFunc: () => Instantiate(_gpsGeoObjectPrefab),
            actionOnGet: obj => obj.gameObject.SetActive(true),
            actionOnRelease: obj => obj.gameObject.SetActive(false),
            actionOnDestroy: obj => Destroy(obj.gameObject),
            collectionCheck: true,
            defaultCapacity: 10,
            maxSize: _maxGeoObjectCount > 0 ? _maxGeoObjectCount : 100
        );
    }

    private async void Start()
    {
        Camera.onPreRender += preRender;
        
        _wpsGeoObjectParent = new GameObject("WPS Labels");
        _wpsGeoObjectParent.transform.SetParent(_xrOrigin.TrackablesParent.transform);
        _wpsGeoObjectParent.transform.localPosition = Vector3.zero;
        _wpsGeoObjectParent.transform.localRotation = Quaternion.identity;
        
        _gpsGeoObjectParent = new GameObject("GPS Labels");
        _gpsGeoObjectParent.transform.SetParent(_xrOrigin.TrackablesParent.transform);
        _gpsGeoObjectParent.transform.localPosition = Vector3.zero;
        _gpsGeoObjectParent.transform.localRotation = Quaternion.identity;
        
        _compassWorldPose.onClick.AddListener(() => { _wpsGeoObjectParent.SetActive(!_wpsGeoObjectParent.activeSelf); });
        _compassMagneticHeading.onClick.AddListener(() => { _gpsGeoObjectParent.SetActive(!_gpsGeoObjectParent.activeSelf); });
        
        _coverageClientManager.QueryRadius = 1000;
        _coverageClientManager.UseCurrentLocation = true;
        
        // Start coroutine to capture initial heading when compass is ready
        await CaptureInitialHeadingAsync();
        
        _coverageClientManager.TryGetCoverage(OnTryGetCoverage);
    }
    
    private async Task CaptureInitialHeadingAsync()
    {   
        // Wait a sec for compass to stabilize
        await Task.Delay(1000);
        
        // Capture the initial heading
        _initialHeading = Input.compass.trueHeading;
    }

    void preRender(Camera cam)
    {
        // Translate and Rotate WPS(orange) and GPS(blue) positioned objects:
        foreach (var pair in _managedWpsGeoObjectPairs.Values)
        {
            pair.WpsObject.UpdateVisibleObjectsTransform(_trackingCamera.transform);
            pair.GpsObject.UpdateVisibleObjectsTransform(_trackingCamera.transform);
        }
    }

    private void Update()
    {
        // Update the visibility of WPS(orange) and GPS(blue) positioned objects:
        foreach (var pair in _managedWpsGeoObjectPairs.Values)
        {
            pair.WpsObject.UpdateVisibleObjectsVisibility(_trackingCamera.transform, _coverageClientManager.QueryRadius);
            pair.GpsObject.UpdateVisibleObjectsVisibility(_trackingCamera.transform, _coverageClientManager.QueryRadius);
        }
    }

    private async void OnTryGetCoverage(AreaTargetsResult result)
    {
        if (_cancellationTokenSource.IsCancellationRequested)
        {
            return;
        }
        
        if (result.Status == ResponseStatus.Success)
        {
            _requestStatusText.text = "Response: " + result.AreaTargets.Count +
                                      " targets(s) found within " + result.QueryRadius +
                                      "m of [" + result.QueryLocation.Latitude +
                                      ", " + result.QueryLocation.Longitude + "]";

            UpdateManagedGeoObjects(result);

            try
            {
                // Wait for the next Query
                await Task.Delay(_queryCoverageApiInterval, _cancellationTokenSource.Token);
                // Re-query the coverage
                _coverageClientManager.TryGetCoverage(OnTryGetCoverage);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("CoverageQuery task was canceled.");
            }
        }
        else
        {
            _requestStatusText.text = "Response: " + result.Status;
        }
    }
    
    private void UpdateManagedGeoObjects(AreaTargetsResult result)
    {
        // Sort the results by distance to the query location
        var sortedTargets = result.AreaTargets.OrderBy(a => 
            a.Area.Centroid.Distance(result.QueryLocation)).ToList();
        
        // Take the first N results up to the max count
        var max = _maxGeoObjectCount == 0 ? sortedTargets.Count : Math.Min(_maxGeoObjectCount, sortedTargets.Count);
        var newTargets = sortedTargets.Take(max).ToList();

        // Create a set of the new target IDs
        var newTargetIds = new HashSet<string>(newTargets.Select(t => t.Target.Identifier));
        
        // Find all GeoObjects that are not in the new target list
        var idsToRemove = _managedWpsGeoObjectPairs.Keys.Where(id => !newTargetIds.Contains(id)).ToList();
        
        // Release any GeoObjects that are no longer needed
        foreach (var id in idsToRemove)
        {
            ReleaseGeoObjectsOf(id);
        }

        // Register or Update any new GeoObjects based on the new target list
        foreach (var areaTarget in newTargets)
        {
            var id = areaTarget.Target.Identifier;

            if (_managedWpsGeoObjectPairs.ContainsKey(id))
            {
                // Update existing GeoObject
                UpdateGeoObjectsOf(id, areaTarget);
            }
            else
            {
                // Get pooled GeoObject from pool or create a new one
                GetGeoObjectsOf(id, areaTarget);
            }
        }
    }

    private void GetGeoObjectsOf(string id, AreaTarget areaTarget)
    {
        var newWpsGeoObject = _wpsGeoObjectPool.Get();
        var newGpsGeoObject = _gpsGeoObjectPool.Get();
        var newGeoObjectPair = new GeoObjectPair(newWpsGeoObject, newGpsGeoObject);
        newGpsGeoObject.transform.SetParent(_gpsGeoObjectParent.transform);
        
        Debug.Log($"Creating WPS and GPS GeoObject: {areaTarget.Target.Name}");
        
        newWpsGeoObject.Setup(new GeoObjectSetupParameters(areaTarget, _wpsGeoObjectParent, _worldPositioningObjectHelper, _initialHeading));
        newGpsGeoObject.Setup(new GeoObjectSetupParameters(areaTarget, _gpsGeoObjectParent, _worldPositioningObjectHelper, _initialHeading));
        
        _managedWpsGeoObjectPairs.Add(id, newGeoObjectPair);
        
        newWpsGeoObject.UpdateGeoObjectPosition(_trackingCamera.transform);
        newGpsGeoObject.UpdateGeoObjectPosition(_trackingCamera.transform);
        
        OnGeoObjectAdded?.Invoke(newGeoObjectPair);
    }

    private void UpdateGeoObjectsOf(string id, AreaTarget areaTarget)
    {
        var wpsObjToUpdate = _managedWpsGeoObjectPairs[id].WpsObject;
        var gpsObjToUpdate = _managedWpsGeoObjectPairs[id].GpsObject;
        var geoObjectPair = new GeoObjectPair(wpsObjToUpdate, gpsObjToUpdate);
        
        Debug.Log($"Updating WPS and GPS GeoObjects: {wpsObjToUpdate.Name}");
        
        wpsObjToUpdate.UpdateData(areaTarget);
        gpsObjToUpdate.UpdateData(areaTarget);
        
        wpsObjToUpdate.UpdateGeoObjectPosition(_trackingCamera.transform);
        gpsObjToUpdate.UpdateGeoObjectPosition(_trackingCamera.transform);
        
        OnGeoObjectUpdated?.Invoke(geoObjectPair);
    }

    private void ReleaseGeoObjectsOf(string id)
    {
        var wpsObjToRemove = _managedWpsGeoObjectPairs[id].WpsObject;
        var gpsObjToRemove = _managedWpsGeoObjectPairs[id].GpsObject;
        var geoObjectPair = new GeoObjectPair(wpsObjToRemove, gpsObjToRemove);
        
        Debug.Log($"Removing WPS and GPS GeoObjects: {wpsObjToRemove.Name}");
        
        wpsObjToRemove.TearDown();
        gpsObjToRemove.TearDown();
        
        OnGeoObjectRemoved?.Invoke(geoObjectPair);
            
        _managedWpsGeoObjectPairs.Remove(id);
        _wpsGeoObjectPool.Release(wpsObjToRemove);
        _gpsGeoObjectPool.Release(gpsObjToRemove);
    }

    private void OnDestroy()
    {
        _compassWorldPose.onClick.RemoveAllListeners();
        _compassMagneticHeading.onClick.RemoveAllListeners();
        
        Camera.onPreRender -= preRender;
        
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }
}
