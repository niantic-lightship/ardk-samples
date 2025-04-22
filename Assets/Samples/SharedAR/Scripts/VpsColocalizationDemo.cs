// Copyright 2022-2025 Niantic.

using System;
using System.Collections;
using Niantic.Lightship.AR.LocationAR;
using UnityEngine;
using UnityEngine.UI;
using Niantic.Lightship.SharedAR.Colocalization;
using Unity.Netcode;
using Niantic.Lightship.SharedAR.Netcode;
using UnityEngine.Networking;
using NetworkTransport = Unity.Netcode.NetworkTransport;

namespace  Niantic.Lightship.AR.Samples
{
    public class VpsColocalizationDemo : MonoBehaviour
    {
        public static VpsColocalizationDemo Instance { get; private set; }

        private ARLocationManager _arLocationManager;

        private ARLocationManager LocationManager
        {
            get
            {
                if (_arLocationManager == null)
                {
                    _arLocationManager = FindObjectOfType<ARLocationManager>();
                }

                return _arLocationManager;
            }
        }
        
        [SerializeField]
        private VpsCoverageTargetListManager _vpsCoverageTargetListManager;

        [SerializeField]
        private AnimationToggle _panelToggle;

        [SerializeField]
        private SharedSpaceManager _sharedSpaceManager;

        [SerializeField]
        private GameObject _netButtonsPanel;

        [SerializeField]
        private GameObject _joinButtons;
        
        [SerializeField]
        private Button _exitButton;

        [SerializeField]
        private GameObject _lightShipNetcodeTransportStatsDisplayPrefab;

        [SerializeField]
        private Text _localizationStatusText;

        [SerializeField]
        private GameObject _localizationStatusPanel;

        [InspectorName("In-Editor Payloads")]
        [SerializeField]
        private string _inEditorPayload;
        
        private LightshipNetcodeTransportStatsDisplay _statsPanel;

        private LightshipNetcodeTransportStatsDisplay StatsPanel
        {
            get
            {
                if (_statsPanel == null)
                {
                    _statsPanel = FindFirstObjectByType<LightshipNetcodeTransportStatsDisplay>();
                }
                return _statsPanel;
            }
        }

        private const String _roomNamePrefix = "vpsColocalizationExample_";

        private bool _LastTrackingState = false;

        void Awake()
        {
            Instance = this;
        }

        // Start is called before the first frame update
        void Start()
        {
            if (_lightShipNetcodeTransportStatsDisplayPrefab == null)
            {
                Debug.LogError("Please assign lightship netcode transport stats prefab.");
                return;
            }
            
            InitializeScene();
        }

        private void InitializeScene()
        {
            _sharedSpaceManager.sharedSpaceManagerStateChanged += OnColocalizationTrackingStateChanged;
            _vpsCoverageTargetListManager.OnWayspotDefaultAnchorButtonPressed += OnLocationSelected;

            Debug.Log("Starting SharedAR with VPS");
            _LastTrackingState = false;
            _vpsCoverageTargetListManager.gameObject.SetActive(true);
            _panelToggle.OpenState();
            _netButtonsPanel.SetActive(false);
            _joinButtons.SetActive(true);
            _exitButton.gameObject.SetActive(false);
            _localizationStatusPanel.SetActive(false);
            if (_sharedSpaceManager.GetColocalizationType() ==
                SharedSpaceManager.ColocalizationType.MockColocalization)
            {                
                // Hide coverage list panel and show connction button
                _vpsCoverageTargetListManager.gameObject.SetActive(false);
                _panelToggle.CloseState(); // hide the panel for location search
                // Set room to connect
                var vpsTrackingOptions = ISharedSpaceTrackingOptions.CreateMockTrackingOptions();
                var roomOptions = ISharedSpaceRoomOptions.CreateLightshipRoomOptions(
                    _roomNamePrefix + "SkippingVpsRoom",32, "vps colocalization demo");
                _sharedSpaceManager.StartSharedSpace(vpsTrackingOptions, roomOptions);
            }
            else if (Application.isEditor && !string.IsNullOrEmpty(_inEditorPayload))
            {
                StartCoroutine(StartInEditorCoroutine());
            }
        }

        private IEnumerator StartInEditorCoroutine()
        {
            Debug.LogWarning("Skipping coverage selection in favor of provided payload.");
            _vpsCoverageTargetListManager.gameObject.SetActive(false);
            _panelToggle.CloseState(); // hide the panel for location search
            _panelToggle.gameObject.SetActive(false);
            // Add some wait here, or initializing SharedAR networking might fail in editor because this is called immediately after networking was shutdown.
            yield return new WaitForSeconds(0.2f);
            OnLocationSelected(new VpsCoverageTargetListManager.WayspotSelectedArgs { Payload = _inEditorPayload, Name = "Editor Payload" });
        }

        private void CleanupScene()
        {
            _sharedSpaceManager.DestroySharedArOrigin();
            // Stop Tracking and this will destroy the anchor
            LocationManager.StopTracking();

            _sharedSpaceManager.sharedSpaceManagerStateChanged -= OnColocalizationTrackingStateChanged;
            _vpsCoverageTargetListManager.OnWayspotDefaultAnchorButtonPressed -= OnLocationSelected;
            if (NetworkManager.Singleton != null)
            {
                Debug.Log("Remove the Callbacks on NetworkManager");
                NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
                NetworkManager.Singleton.OnClientStarted -= OnClientStarted;
                NetworkManager.Singleton.OnConnectionEvent -= OnConnectionEvent;
                // shutdown NetworkManager
                NetworkManager.Singleton.Shutdown();
            }
            
            if (_netButtonsPanel != null)
               _netButtonsPanel.SetActive(false);
        }

        private void OnLocationSelected(VpsCoverageTargetListManager.WayspotSelectedArgs location)
        {
            if (String.IsNullOrEmpty(location.Payload))
            {
                Debug.LogWarning("The selected location does not have a default anchor");
                return;
            }

            // Start tracking and set Room to join based on anchor payload
            var vpsTrackingOptions = ISharedSpaceTrackingOptions.CreateVpsTrackingOptions(location.Payload);
            var roomOptions= ISharedSpaceRoomOptions.CreateVpsRoomOptions(
                vpsTrackingOptions, _roomNamePrefix, 32, "vps colocaization demo");
            
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            NetworkManager.Singleton.OnClientStarted += OnClientStarted;
            NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;
            
            _sharedSpaceManager.StartSharedSpace(vpsTrackingOptions, roomOptions);
            _vpsCoverageTargetListManager.gameObject.SetActive(false);
            _vpsCoverageTargetListManager.CloseList();
            _localizationStatusPanel.SetActive(true);
            _localizationStatusText.text = "NOT TRACKING";
        }
        
        private void OnServerStarted()
        {
            Debug.Log("Netcode server ready");
            _statsPanel = Instantiate(_lightShipNetcodeTransportStatsDisplayPrefab).GetComponent<LightshipNetcodeTransportStatsDisplay>();
            var networkObject = _statsPanel.gameObject.GetComponent<NetworkObject>();
            networkObject.Spawn(destroyWithScene: true);
        }

        private void InitializeStatsPanel()
        {
            var canvas = FindFirstObjectByType<Canvas>().transform;
            StatsPanel.transform.SetParent(canvas);
            var rectTransform = StatsPanel.GetComponent<RectTransform>();
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(1f, 0.5f);
            rectTransform.anchorMin = new Vector2(1f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-250f, 450f);
            rectTransform.sizeDelta = new Vector2(500f, 385f);
            rectTransform.localScale = Vector3.one;
        }
        
        private void OnClientStarted()
        {
            _netButtonsPanel.SetActive(true);
            _exitButton.gameObject.SetActive(true);
        }

        private void OnColocalizationTrackingStateChanged(SharedSpaceManager.SharedSpaceManagerStateChangeEventArgs args)
        {
            if (args.Tracking)
            {
                Debug.Log("ARLocation TRACKING");
                _localizationStatusText.text = "TRACKING";

                // Show the Host/Client select panel if not in a room
                // IsConnectedClient is always false on this host, check ifhost too
                // Also only update the UI if the state has changed from the last event
                Debug.Log($"LastTrackingState: {_LastTrackingState}, IsConnectedClient: {NetworkManager.Singleton.IsConnectedClient}, IsHost: {NetworkManager.Singleton.IsHost}");
                if (!_LastTrackingState && !NetworkManager.Singleton.IsConnectedClient && !NetworkManager.Singleton.IsHost)
                {
                    _netButtonsPanel.SetActive(true);
                }
            }
            else
            {
                if(_localizationStatusText != null){
                    _localizationStatusText.text = "NOT TRACKING";
                }
            }

            _LastTrackingState = args.Tracking;
        }

        public void OnJoinAsHostClicked()
        {
            if (NetworkManager.Singleton.StartHost())
            {
                _joinButtons.SetActive(false);
            }
        }

        public void OnJoinAsClientClicked()
        {
            if (NetworkManager.Singleton.StartClient())
            {
                _joinButtons.SetActive(false);
            }
        }

        public void OnExitClicked()
        {
            Debug.Log("Exit clicked");
            _exitButton.gameObject.SetActive(false);
            
            // Leave room first to invoke the network disconnection event
            if (NetworkManager.Singleton.IsConnectedClient)
            {
                _sharedSpaceManager.LeaveRoom();
            }
            // if user wants to exit before connected to the room, then 
            else
            {
                RestartSession();
            }
        }
        
        private void RestartSession()
        {
            CleanupScene();
            InitializeScene();
        }
        
        private void OnConnectionEvent(NetworkManager networkManager, ConnectionEventData eventData)
        {
            if (eventData.EventType == ConnectionEvent.ClientConnected)
            {
                Debug.Log($"Client connected: {eventData.ClientId}, isSelf: {networkManager.LocalClient.ClientId == eventData.ClientId}");
                // Only toggle UI elements when it's self connection event.
                if (eventData.ClientId == networkManager.LocalClient.ClientId)
                {
                    _netButtonsPanel.SetActive(true);
                    _exitButton.gameObject.SetActive(true);
                    InitializeStatsPanel();
                    StatsPanel.Show();
                }
            } else if (eventData.EventType == ConnectionEvent.ClientDisconnected)
            {
                Debug.Log($"Client disconnected: {eventData.ClientId}, isSelf: {networkManager.LocalClient.ClientId == eventData.ClientId}");
                if (networkManager.IsHost && eventData.ClientId != networkManager.LocalClient.ClientId)
                {
                    // ignore other clients' disconnect event
                    return;
                }
                Debug.Log("Disconnected from Server");
                RestartSession();
            }
        }
        private void OnDestroy()
        {
            // Do nothing if the prefab is not assigned
            if (_lightShipNetcodeTransportStatsDisplayPrefab == null)
            {
                return;
            }
            
            CleanupScene();
            // destroy NetworkManager when switching the scene
            if (NetworkManager.Singleton != null)
            {
                Destroy(NetworkManager.Singleton.gameObject);
            }
        }
    }
}
