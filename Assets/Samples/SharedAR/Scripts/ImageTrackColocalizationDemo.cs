// Copyright 2023 Niantic, Inc. All Rights Reserved.

using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Niantic.Lightship.SharedAR.Colocalization;
using UnityEngine.UI;

namespace Niantic.Lightship.AR.Samples
{
    public class ImageTrackColocalizationDemo : MonoBehaviour
    {
        public static ImageTrackColocalizationDemo Instance { get; private set; }

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
        private InputField _roomNameTextField;

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

            Debug.Log("Starting Colocalization+Peer positioning");
            _netButtonsPanel.SetActive(false);

            // Call startTracking delayed
            StartCoroutine(StartTracking());

        }

        IEnumerator StartTracking()
        {
            yield return null;
            _sharedSpaceManagerManager.StartTracking("");
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
                _netButtonsPanel.SetActive(true);
                // create an origin marker object and set under the sharedAR origin
                var originMarkerObj = Instantiate(_sharedRootMarkerPrefab, new Vector3(), Quaternion.identity);
                originMarkerObj.transform.parent = _sharedSpaceManagerManager._sharedArOriginObject.transform;
            }
            else
            {
                Debug.Log($"Image tracking not tracking?");
            }
        }

        public void OnJoinAsHostClicked()
        {
            _sharedSpaceManagerManager.PrepareRoom(
                _roomNameTextField.text,
                32,
                "vps colocalization demo"
            );
            NetworkManager.Singleton.StartHost();
            _netButtonsPanel.SetActive(false);
        }

        public void OnJoinAsClientClicked()
        {
            _sharedSpaceManagerManager.PrepareRoom(
                _roomNameTextField.text,
                32,
                "vps colocalization demo"
            );
            NetworkManager.Singleton.StartClient();
            _netButtonsPanel.SetActive(false);
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
