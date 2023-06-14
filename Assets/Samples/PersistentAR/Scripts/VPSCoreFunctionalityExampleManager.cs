using Niantic.Lightship.AR.Subsystems;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARSubsystems;

public class VPSCoreFunctionalityExampleManager : MonoBehaviour
{
    [SerializeField]
    private ARPersistentAnchorManager _persistentAnchorManager;

    [SerializeField]
    private string _base64Payload;

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

    public void Start()
    {
        if (_persistentAnchorManager == null || string.IsNullOrEmpty(_base64Payload))
        {
            Debug.LogError("Could not track anchor");
            return;
        }

        if (_debugText != null)
        {
            _debugText.text = "Started";
        }

        if (_payloadText != null)
        {
            _payloadText.text = $"Added anchor: {_base64Payload}";
        }

        var payload = new ARPersistentAnchorPayload(_base64Payload);
        _persistentAnchorManager.TryTrackAnchor(payload, out _arPersistentAnchor);
    }

    private void OnDisable()
    {
        _persistentAnchorManager.arPersistentAnchorStateChanged -= HandleArPersistentAnchorStateChanged;
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

    public void RestartTracking()
    {
        Destroy(_arPersistentAnchor.gameObject);
        if (_debugText != null)
        {
            _debugText.text = "Stopped";
        }
    }
}
