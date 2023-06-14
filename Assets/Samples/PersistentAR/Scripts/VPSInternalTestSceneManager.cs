using Niantic.Lightship.AR.Subsystems;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARSubsystems;

#if !UNITY_EDITOR && UNITY_ANDROID
using UnityEngine.Android;
#endif

public class VPSInternalTestSceneManager : MonoBehaviour
{
    [SerializeField]
    private ARPersistentAnchorManager _persistentAnchorManager;

    [SerializeField]
    private InputField _anchorPayload;

    [SerializeField]
    private Text _payloadText;

    [SerializeField]
    private Text _debugText;

    [SerializeField]
    private GameObject _gameObject;

    private ARPersistentAnchor _arPersistentAnchor;

    private void OnEnable()
    {
        _persistentAnchorManager.arPersistentAnchorStateChanged += HandleArPersistentAnchorStateChanged;
    }

    protected void Start()
    {
#if !UNITY_EDITOR && UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            var androidPermissionCallbacks = new PermissionCallbacks();
            androidPermissionCallbacks.PermissionDenied += permissionName =>
            {
                if (permissionName == "android.permission.ACCESS_FINE_LOCATION")
                {
                    _debugText.text = "Location Permission Denied";
                }
            };
            androidPermissionCallbacks.PermissionDeniedAndDontAskAgain += permissionName =>
            {
                if (permissionName == "android.permission.ACCESS_FINE_LOCATION")
                {
                    _debugText.text = "Location Permission Denied By Default";
                }
            };

            Permission.RequestUserPermission(Permission.FineLocation, androidPermissionCallbacks);
        }
#endif
    }

    private void OnDisable()
    {
        _persistentAnchorManager.arPersistentAnchorStateChanged -= HandleArPersistentAnchorStateChanged;
    }

    // Start is called before the first frame update
    public void StartTracking()
    {
        var anchorPayload = _anchorPayload.text;
        if (_persistentAnchorManager == null)
        {
            Debug.LogError("No anchor manager");
            return;
        }

        if (string.IsNullOrEmpty(anchorPayload))
        {
            Debug.LogError("Empty payload");
            return;
        }

        if (_debugText != null)
        {
            _debugText.text = "Started";
        }

        if (_payloadText != null)
        {
            _payloadText.text = $"Added anchor: {anchorPayload}";
        }

        var payload = new ARPersistentAnchorPayload(anchorPayload);
        _persistentAnchorManager.TryTrackAnchor(payload, out _arPersistentAnchor);
    }

    public void RestartTracking()
    {
        if (_arPersistentAnchor)
        {
            Destroy(_arPersistentAnchor.gameObject);
        }
        if (_debugText != null)
        {
            _debugText.text = "Stopped";
        }
    }

    private void HandleArPersistentAnchorStateChanged(ARPersistentAnchorStateChangedEventArgs args)
    {
        if (_arPersistentAnchor.trackingState == TrackingState.Tracking)
        {
            Instantiate(_gameObject, _arPersistentAnchor.transform, false);
            _arPersistentAnchor = args.arPersistentAnchor;
        }
        if (_debugText != null && _arPersistentAnchor)
        {
            _debugText.text = _arPersistentAnchor.trackingState.ToString();
        }
    }
}
