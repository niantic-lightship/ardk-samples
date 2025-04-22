// Copyright 2022-2025 Niantic.
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Niantic.Lightship.AR.LocationAR;
using Niantic.Lightship.AR.PersistentAnchors;
using Niantic.Lightship.SharedAR.Networking;
using Niantic.Lightship.SharedAR.Rooms;
using UnityEngine;
using UnityEngine.UI;

public class NoNetcodeDemo : MonoBehaviour
{
    [SerializeField] private GameObject _nameInputPanel;
    [SerializeField] private GameObject _requestAreaPanel;
    [SerializeField] private GameObject _errorPanel;
    [SerializeField] private Text _errorBody;

    [SerializeField] private VpsCoverageTargetListManager _vpsCoverageTargetListManager;

    [SerializeField] private ARLocationManager _arLocationManager;

    [SerializeField] private InputField _nameInputField;

    [SerializeField] private Camera _playerCamera;

    [SerializeField] private GameObject _playerPrefab;

    [SerializeField] private Text _connectedPlayersText;

    private ARLocation _arLocation = null;
    private IRoom _currentRoom = null;
    private INetworking _networking = null;

    private Dictionary<PeerID, GameObject> _peerAvatars;
    private Dictionary<PeerID, string> _connectedPlayers;

    private Color _playerColor;
    private Coroutine _positionUpdateRoutine = default;
    private readonly WaitForSeconds _positionUpdateDelay = new WaitForSeconds(0.033f); // 30fps target
    private readonly WaitForSeconds _connectivityCheckSeconds = new WaitForSeconds(1f);
    private Coroutine _localizingRoutine;

    private void Awake()
    {
        _peerAvatars = new();
        _connectedPlayers = new();
    }

    private void Start()
    {
        _vpsCoverageTargetListManager.OnWayspotDefaultAnchorButtonPressed += OnLocationSelected;

        _arLocationManager.locationTrackingStateChanged += OnLocationTrackingStateChanged;
        _nameInputPanel.SetActive(true);
    }

    // also used by Back button
    public void Cleanup()
    {
        if (_currentRoom != null)
        {
            _currentRoom.Leave();
            _currentRoom = null;
        }

        if (_networking != null)
        {
            _networking.NetworkEvent -= OnNetworkEvent;
            _networking.PeerAdded -= OnPeerAdded;
            _networking.PeerRemoved -= OnPeerRemoved;
            _networking.DataReceived -= OnDataReceived;

            _networking = null;
        }
        
        _arLocationManager.locationTrackingStateChanged -= OnLocationTrackingStateChanged;
        _arLocationManager.StopTracking();
        _arLocation = null;

        // turn off any panels
        _vpsCoverageTargetListManager.CloseList();
        _requestAreaPanel.SetActive(false);
        _nameInputPanel.SetActive(false);

        // stop broadcasting position updates
        if (_positionUpdateRoutine != null)
        {
            StopCoroutine(_positionUpdateRoutine);
            _positionUpdateRoutine = null;
        }

        // remove all avatar instances
        foreach (var avatar in _peerAvatars)
        {
            Destroy(avatar.Value);
        }

        _peerAvatars.Clear();

        _connectedPlayersText.text = string.Empty;
        _connectedPlayersText.transform.parent.gameObject.SetActive(false);
        _connectedPlayers.Clear();

        _errorPanel.SetActive(false);
        _errorBody.text = string.Empty;

        if (_localizingRoutine != null)
        {
            StopCoroutine(_localizingRoutine);
            _localizingRoutine = null;
        }
    }

    private void RestartDemo(string restartReason = null)
    {
        Cleanup();

        _nameInputPanel.SetActive(true);
        _arLocationManager.locationTrackingStateChanged += OnLocationTrackingStateChanged;

        if (restartReason != null)
        {
            ShowError(restartReason);
        }
    }

    public void OnNameInputSubmit()
    {
        if (string.IsNullOrEmpty(_nameInputField.text) || string.IsNullOrWhiteSpace(_nameInputField.text))
        {
            ShowError("Please enter your user name before continuing.");
            return;
        }

        _requestAreaPanel.SetActive(true);
        _nameInputPanel.SetActive(false);
    }

    private IEnumerator CheckInternet()
    {
        while (true)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                RestartDemo("Lost network connection");
                break;
            }

            yield return _connectivityCheckSeconds;
        }
    }

    private void OnLocationSelected(VpsCoverageTargetListManager.WayspotSelectedArgs location)
    {
        _vpsCoverageTargetListManager.CloseList();

        // create the ARLocation for the selected anchor and parent it to the ARLocationManager (required)
        _arLocation = new GameObject(location.Name, typeof(ARLocation)).GetComponent<ARLocation>();
        _arLocation.transform.SetParent(_arLocationManager.transform);

        _arLocation.Payload = new ARPersistentAnchorPayload(location.Payload);

        // configure the location manager and localize against our location
        _arLocationManager.SetARLocations(_arLocation);
        _arLocationManager.StartTracking();

        // handle error case if the user loses network connection during localizing attempts
        _localizingRoutine = StartCoroutine(CheckInternet());
    }

    private void OnLocationTrackingStateChanged(ARLocationTrackedEventArgs args)
    {
        if (args.Tracking && _currentRoom == null)
        {
            StopCoroutine(_localizingRoutine);
            _localizingRoutine = null;

            var roomParams = new RoomParams
                (10, args.ARLocation.name, "VPS Multiplayer Demo");

            var roomStatus = RoomManagementService.GetOrCreateRoomForName(roomParams, out _currentRoom);

            if (roomStatus == RoomManagementServiceStatus.Ok)
            {
                Debug.Log($"Joining room: {_currentRoom.RoomParams.Name}");

                _currentRoom.Initialize();
                _networking = _currentRoom.Networking;
                _networking.NetworkEvent += OnNetworkEvent;
                _networking.PeerAdded += OnPeerAdded;
                _networking.PeerRemoved += OnPeerRemoved;
                _networking.DataReceived += OnDataReceived;
                _currentRoom.Join();
            }
        }
        else if (!args.Tracking && _arLocationManager != null ||
                 args.TrackingStateReason == ARLocationTrackingStateReason.Limited)
        {
            // We de-activate the gameObject when we lose tracking.
            // ARLocationManager will not de-activate it
            args.ARLocation.gameObject.SetActive(false);

            RestartDemo($"Lost Tracking: {args.TrackingStateReason}");
        }
    }

    private void OnNetworkEvent(NetworkEventArgs args)
    {
        if (args.networkEvent == NetworkEvents.Connected)
        {
            Debug.Log($"Connected to Network with id {_networking.SelfPeerID}");

            _playerColor = Random.ColorHSV();

            // add our player name to the list and update visuals

            _connectedPlayers.Add(_networking.SelfPeerID, _nameInputField.text);
            RefreshConnectedPlayers();

            // start broadcasting position/rotation updates
            _positionUpdateRoutine = StartCoroutine(ContinuouslySendTransform());

            // passing an empty list to SendData will send our data to all other peers
            var empty = new List<PeerID>();

            // send our player data to existing peers
            var info = JsonUtility.ToJson((_connectedPlayers[_networking.SelfPeerID], _playerColor));

            _networking.SendData(empty, MessageTags.RECEIVE_PLAYER_INFO, Encoding.Unicode.GetBytes(info));
        }

        if (args.networkEvent == NetworkEvents.Disconnected || args.networkEvent == NetworkEvents.ConnectionError)
        {
            RestartDemo($"Problem with Network: {args.networkEvent} : {args.errorCode}");
        }
    }

    private void OnPeerAdded(PeerIDArgs args)
    {
        Debug.Log($"Peer {args.PeerID} connected.");

        var info = JsonUtility.ToJson((_nameInputField.text, _playerColor));

        // send our player info to the peer who just connected
        _networking.SendData(new List<PeerID> { args.PeerID }, MessageTags.RECEIVE_PLAYER_INFO,
            Encoding.Unicode.GetBytes(info));
    }

    private void OnPeerRemoved(PeerIDArgs args)
    {
        // remove avatar for peer
        if (_peerAvatars.TryGetValue(args.PeerID, out GameObject peerAvatar))
        {
            Destroy(peerAvatar);
            _peerAvatars.Remove(args.PeerID);
            _connectedPlayers.Remove(args.PeerID);
            RefreshConnectedPlayers();
        }
    }

    private void OnDataReceived(DataReceivedArgs args)
    {
        string json = string.Empty;

        // NOTE: you'll likely want to use a more performant (de)serialization solution vs json
        using (var ms = args.CreateDataReader())
        {
            json = Encoding.Unicode.GetString(ms.ToArray());
        }

        switch (args.Tag)
        {
            case MessageTags.RECEIVE_PLAYER_INFO:

                var playerInfo = JsonUtility.FromJson<(string Name, Color Color)>(json);

                // add or update avatar for this player
                if (!_peerAvatars.TryGetValue(args.PeerID, out GameObject playerInstance))
                {
                    // create new instance of this player with the provided info as a child of the ARLocation
                    playerInstance = Instantiate(_playerPrefab, _arLocation.transform);

                    // add the instance to our collection of avatars
                    _peerAvatars.Add(args.PeerID, playerInstance);
                }

                // update name and color
                var playerRenderer = playerInstance.GetComponentInChildren<MeshRenderer>();

                if (playerRenderer != null)
                {
                    playerRenderer.material.color = playerInfo.Color;
                }

                if (!_connectedPlayers.ContainsKey(args.PeerID))
                {
                    _connectedPlayers.Add(args.PeerID, playerInfo.Name);
                }
                else
                {
                    _connectedPlayers[args.PeerID] = playerInfo.Name;
                }

                var label = playerInstance.GetComponentInChildren<TextMesh>();

                if (label != null)
                {
                    label.text = _connectedPlayers[args.PeerID];
                }

                RefreshConnectedPlayers();
                break;

            case MessageTags.PLAYER_XFORM_UPDATE:

                if (_peerAvatars.TryGetValue(args.PeerID, out GameObject avatar))
                {
                    (Vector3 position, Quaternion rotation) transformData =
                        JsonUtility.FromJson<(Vector3, Quaternion)>(json);
                    avatar.transform.localPosition = transformData.position;
                    avatar.transform.rotation = transformData.rotation;
                    avatar.transform.up = _arLocation.transform.up;
                }

                break;
        }
    }

    private IEnumerator ContinuouslySendTransform()
    {
        var emptyList = new List<PeerID>(); // passing an empty list to SendData will send our data to all other peers

        while (true)
        {
            // serialize player position relative to anchor root
            var anchorRelativePosition = _arLocation.transform.InverseTransformPoint(_playerCamera.transform.position);

            string position = JsonUtility.ToJson((anchorRelativePosition, _playerCamera.transform.rotation));

            _networking.SendData(emptyList, MessageTags.PLAYER_XFORM_UPDATE,
                Encoding.Unicode.GetBytes(position));

            yield return _positionUpdateDelay;
        }
    }

    private void ShowError(string errorText)
    {
        _errorPanel.SetActive(true);
        _errorBody.text = errorText;
    }

    private void RefreshConnectedPlayers()
    {
        var hasPlayers = _connectedPlayers.Count > 0;

        _connectedPlayersText.transform.parent.gameObject.SetActive(hasPlayers);

        _connectedPlayersText.text = hasPlayers ? string.Join(", ", _connectedPlayers.Values) : string.Empty;
    }

    private static class MessageTags
    {
        public const uint RECEIVE_PLAYER_INFO = 0;
        public const uint PLAYER_XFORM_UPDATE = 1;
    }
}