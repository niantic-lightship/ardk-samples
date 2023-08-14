// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Niantic.Lightship.SharedAR.Colocalization;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Niantic.Lightship.AR.Samples
{
    public class ImageTrackingColocalizationDemo : MonoBehaviour
    {
        public static ImageTrackingColocalizationDemo Instance { get; private set; }

        [SerializeField]
        private SharedSpaceManager _sharedSpaceManagerManager;

        [SerializeField]
        private GameObject _netButtonsPanel;

        [SerializeField]
        private GameObject _endPanel;

        [SerializeField]
        private Text _endPanelText;

        [SerializeField]
        private GameObject _sharedRootMarkerPrefab;

        [SerializeField]
        private InputField _roomNameInputField;

        [SerializeField]
        private Text _roomNameDisplayText;

        private string _roomName;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            _sharedSpaceManagerManager.sharedSpaceManagerStateChanged += OnColocalizationTrackingStateChanged;
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedCallback;

            Debug.Log("Starting Image Tracking Colocalization");
            _netButtonsPanel.SetActive(true);
            _roomNameDisplayText.gameObject.SetActive(false);

        }

        private void OnDestroy()
        {
            _sharedSpaceManagerManager.sharedSpaceManagerStateChanged -= OnColocalizationTrackingStateChanged;
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

        private void OnColocalizationTrackingStateChanged(SharedSpaceManager.SharedSpaceManagerStateChangeEventArgs args)
        {
            if (args.Tracking)
            {
                Debug.Log("Colocalized.");
                // create an origin marker object and set under the sharedAR origin
                Instantiate(_sharedRootMarkerPrefab,
                    _sharedSpaceManagerManager._sharedArOriginObject.transform, false);
            }
            else
            {
                Debug.Log($"Image tracking not tracking?");
            }
        }

        public void StartNewRoom()
        {
            // start tracking. pass empty string for image tracking colocalization.
            _sharedSpaceManagerManager.StartTracking("");

            // generate a new room name. 3 digit number.
            int code = (int)Random.Range(0.0f, 999.0f);
            _roomName =  code.ToString("D3");
            SetupRoomAndUI();

            // start as host
            NetworkManager.Singleton.StartHost();
        }

        public void Join()
        {
            // start tracking. pass empty string for image tracking colocalization.
            _sharedSpaceManagerManager.StartTracking("");

            // set room name from text box
            _roomName = _roomNameInputField.text;
            SetupRoomAndUI();

            // start as client
            NetworkManager.Singleton.StartClient();
        }

        private void SetupRoomAndUI()
        {
            _sharedSpaceManagerManager.PrepareRoom(
                _roomName,
                32,
                ""
            );
            _roomNameDisplayText.text = $"PIN: {_roomName}";
            _netButtonsPanel.SetActive(false);
            _roomNameDisplayText.gameObject.SetActive(true);
        }

        private void OnServerStarted()
        {
            Debug.Log("Netcode server ready");
        }

        private void OnClientConnectedCallback(ulong clientId)
        {
            Debug.Log($"Client connected: {clientId}");
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
            }
        }

        // Handle host disconnected. For now, just show UI to go back home scene
        public void HandleHostDisconnected()
        {
            _endPanelText.text = "Host disconnected";
            _endPanel.SetActive(true);
        }

    }
}
