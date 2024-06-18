// Copyright 2022-2024 Niantic.

using System;
using UnityEngine;
using UnityEngine.UI;
using Niantic.Lightship.SharedAR.Colocalization;
using Unity.Netcode;
using Niantic.Lightship.SharedAR.Netcode;

namespace  Niantic.Lightship.AR.Samples
{
    public class VpsColocalizationDemo : MonoBehaviour
    {
        public static VpsColocalizationDemo Instance { get; private set; }

        [SerializeField]
        private VpsCoverageTargetListManager _vpsCoverageTargetListManager;

        [SerializeField]
        private AnimationToggle _panelToggle;

        [SerializeField]
        private SharedSpaceManager _sharedSpaceManager;

        [SerializeField]
        private GameObject _netButtonsPanel;

        [SerializeField]
        private GameObject _endPanel;

        [SerializeField]
        private LightshipNetcodeTransportStatsDisplay _statsPanel;

        [SerializeField]
        private Text _endPanelText;

        [SerializeField]
        private Text _localizationStatusText;

        [SerializeField]
        private GameObject _localizationStatusPanel;

        [InspectorName("In-Editor Payloads")]
        [SerializeField]
        private string _inEditorPayload;

        private const String _roomNamePrefix = "vpsColocalizationExample_";

        void Awake()
        {
            Instance = this;
        }

        // Start is called before the first frame update
        void Start()
        {
            _sharedSpaceManager.sharedSpaceManagerStateChanged += OnColocalizationTrackingStateChanged;
            _vpsCoverageTargetListManager.OnWayspotDefaultAnchorButtonPressed += OnLocationSelected;
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedCallback;

            Debug.Log("Starting SharedAR with VPS");
            _netButtonsPanel.SetActive(false);
            _statsPanel.Hide();
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
                Debug.LogWarning("Skipping coverage selection in favor of provided payload.");
                _vpsCoverageTargetListManager.gameObject.SetActive(false);
                _panelToggle.CloseState(); // hide the panel for location search
                _panelToggle.gameObject.SetActive(false);
                OnLocationSelected(_inEditorPayload);
            }
        }

        private void OnDestroy()
        {
            _sharedSpaceManager.sharedSpaceManagerStateChanged -= OnColocalizationTrackingStateChanged;
            _vpsCoverageTargetListManager.OnWayspotDefaultAnchorButtonPressed -= OnLocationSelected;
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedCallback;
                // shutdown and destroy NetworkManager when switching the scene
                NetworkManager.Singleton.Shutdown();
                Destroy(NetworkManager.Singleton.gameObject);
            }
        }

        private void OnLocationSelected(string defaultPayloadToSet)
        {
            if (String.IsNullOrEmpty(defaultPayloadToSet))
            {
                Debug.LogWarning("The selected location does not have a default anchor");
                return;
            }

            // Start tracking and set Room to join based on anchor payload
            var vpsTrackingOptions = ISharedSpaceTrackingOptions.CreateVpsTrackingOptions(defaultPayloadToSet);
            var roomOptions= ISharedSpaceRoomOptions.CreateVpsRoomOptions(
                vpsTrackingOptions, _roomNamePrefix, 32, "vps colocaization demo");
            _sharedSpaceManager.StartSharedSpace(vpsTrackingOptions, roomOptions);
            _vpsCoverageTargetListManager.gameObject.SetActive(false);
            _localizationStatusPanel.SetActive(true);
            _localizationStatusText.text = "NOT TRACKING";
        }

        private void OnColocalizationTrackingStateChanged(SharedSpaceManager.SharedSpaceManagerStateChangeEventArgs args)
        {
            if (args.Tracking)
            {
                Debug.Log("ARLocation TRACKING");
                _localizationStatusText.text = "TRACKING";

                // Show the Host/Client select panel if not in a room
                // IsConnectedClient is always false on this host, check ifhost too
                if (!NetworkManager.Singleton.IsConnectedClient && !NetworkManager.Singleton.IsHost)
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
        }

        public void OnJoinAsHostClicked()
        {
            NetworkManager.Singleton.StartHost();
            _netButtonsPanel.SetActive(false);
        }

        public void OnJoinAsClientClicked()
        {
            NetworkManager.Singleton.StartClient();
            _netButtonsPanel.SetActive(false);
        }

        private void OnServerStarted()
        {
            Debug.Log("Netcode server ready");
            _statsPanel.Show();
        }

        private void OnClientConnectedCallback(ulong clientId)
        {
            Debug.Log($"Client connected: {clientId}");
            _statsPanel.Show();
        }

        // Handle network disconnect
        private void OnClientDisconnectedCallback(ulong clientId)
        {
            var selfId = NetworkManager.Singleton.LocalClientId;
            if (NetworkManager.Singleton)
            {
                if (NetworkManager.Singleton.IsHost && clientId != NetworkManager.ServerClientId)
                {
                    // ignore other clients' disconnect event
                    return;
                }
                // show the UI panel for ending
                _endPanelText.text = "Disconnected from network";
                _endPanel.SetActive(true);
                _statsPanel.Hide();
            }
        }

        // Handle host disconnected. For now, just show UI to go back home scene
        public void HandleHostDisconnected()
        {
            _endPanelText.text = "Host disconnected";
            _endPanel.SetActive(true);
            _statsPanel.Hide();
        }
    }
}
