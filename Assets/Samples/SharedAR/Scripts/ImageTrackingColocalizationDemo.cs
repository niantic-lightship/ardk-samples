// Copyright 2022-2025 Niantic.

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
        private SharedSpaceManager _sharedSpaceManager;

        [SerializeField]
        private GameObject _netButtonsPanel;

        [SerializeField]
        private GameObject _endPanel;

        [SerializeField]
        private GameObject _targetImageInstructionPanel;

        [SerializeField]
        private Text _endPanelText;

        [SerializeField]
        private GameObject _sharedRootMarkerPrefab;

        [SerializeField]
        private InputField _roomNameInputField;

        [SerializeField]
        private Text _roomNameDisplayText;

        [SerializeField]
        private Texture2D _targetImage;

        [FormerlySerializedAs("_targetImageSize")]
        [SerializeField]
        private float _targetImageWidthInMeters;

        private string _roomName;

        private bool _startAsHost;
        
        private bool _shutDown;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            _sharedSpaceManager.sharedSpaceManagerStateChanged += OnColocalizationTrackingStateChanged;
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedCallback;

            Debug.Log("Starting Image Tracking Colocalization");
            _netButtonsPanel.SetActive(true);
            _roomNameDisplayText.gameObject.SetActive(false);
            _targetImageInstructionPanel.SetActive(false);

            _roomNameInputField.onValueChanged.AddListener(OnPinValueChane);

        }
        
        void OnDestroy()
        {
            ShutDown();
        }

        public void ShutDown()
        {
            if (_shutDown) return;
            
            _sharedSpaceManager.sharedSpaceManagerStateChanged -= OnColocalizationTrackingStateChanged;
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedCallback;
                // shutdown and destroy NetworkManager when switching the scene
                NetworkManager.Singleton.Shutdown();
                Destroy(NetworkManager.Singleton.gameObject);
            }

            _shutDown = true;
        }

        private void OnColocalizationTrackingStateChanged(SharedSpaceManager.SharedSpaceManagerStateChangeEventArgs args)
        {
            if (args.Tracking)
            {
                Debug.Log("Colocalized.");
                // Hide the target image instruction panel
                _targetImageInstructionPanel.SetActive(false);

                // create an origin marker object and set under the sharedAR origin
                Instantiate(_sharedRootMarkerPrefab,
                    _sharedSpaceManager.SharedArOriginObject.transform, false);

                // Start networking
                if (_startAsHost)
                {
                    NetworkManager.Singleton.StartHost();
                }
                else
                {
                    NetworkManager.Singleton.StartClient();
                }
            }
            else
            {
                Debug.Log($"Image tracking not tracking?");
            }
        }

        public void StartNewRoom()
        {
            var imageTrackingOptions = ISharedSpaceTrackingOptions.CreateImageTrackingOptions(
                _targetImage, _targetImageWidthInMeters);

            // generate a new room name 3 digit number
            int code = (int)Random.Range(0.0f, 999.0f);
            _roomName = code.ToString("D3");
            var roomOptions = SetupRoomAndUI();

            _sharedSpaceManager.StartSharedSpace(imageTrackingOptions, roomOptions);

            // start as host
            _startAsHost = true;
        }

        public void Join()
        {
            var imageTrackingOptions = ISharedSpaceTrackingOptions.CreateImageTrackingOptions(
                _targetImage, _targetImageWidthInMeters);

            //set room name from text box
            _roomName = _roomNameInputField.text;
            var roomOptions = SetupRoomAndUI();

            _sharedSpaceManager.StartSharedSpace(imageTrackingOptions, roomOptions);

            // start as client
            _startAsHost = false;
        }

        private ISharedSpaceRoomOptions SetupRoomAndUI()
        {
            // Update UI
            _roomNameDisplayText.text = $"PIN: {_roomName}";
            _netButtonsPanel.SetActive(false);
            _roomNameDisplayText.gameObject.SetActive(true);
            _targetImageInstructionPanel.SetActive(true);

            //Create a room options and return it

            return ISharedSpaceRoomOptions.CreateLightshipRoomOptions(
                _roomName, 32, "image tracking colocalization demo");
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
        //Enforce 10 character limit on room name
        private void OnPinValueChane(string newPin)
        {
            if (newPin.Length > 6)
            {
                _roomNameInputField.text = newPin.Substring(0, 6);
            }
            else
            {
                _roomNameInputField.text = newPin;
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
