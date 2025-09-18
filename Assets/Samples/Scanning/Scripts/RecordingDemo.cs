// Copyright 2022-2025 Niantic.
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Niantic.ARDK.AR.Scanning;
using Niantic.Lightship.AR.Scanning;
using Unity.SharpZipLib.Utils;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace Niantic.Lightship.AR.Samples
{
    public class RecordingDemo : MonoBehaviour
    {
        [Tooltip("The manager used to perform the scanning")]
        [SerializeField]
        private ARScanningManager _arScanningManager;
        
        [Tooltip("Slider to set framerate")]
        [SerializeField]
        private Slider _framerateSlider;

        [Tooltip("Text of current framerate")]
        [SerializeField]
        private Text _framerateText;

        [Tooltip("Slider to set max recording time per file")]
        [SerializeField]
        private Slider _maxTimePerChunkSlider;

        [Tooltip("Text of current max recording time per file")]
        [SerializeField]
        private Text _maxTimePerChunkText;
        
        [Tooltip("The manager used to render the device camera feed")]
        [SerializeField]
        private ARCameraManager _arCameraManager;
        
        [Tooltip("Scan Panel")]
        [SerializeField]
        private GameObject _performScanPanel;

        [Tooltip("Button to export a scan")]
        [SerializeField]
        private GameObject _saveScanPanel;

        [Tooltip("Export Panel")]
        [SerializeField]
        private GameObject _exportScanPanel;

        [Tooltip("Export Panel title text to show export status")]
        [SerializeField]
        private Text _exportScanPanelTitleText;

        [Tooltip("Export Panel body text to show dataset path")]
        [SerializeField]
        private Text _exportScanPanelBodyText;

        [Tooltip("Start Scan Button")]
        [SerializeField]
        private Button _startScanButton;
        
        [Tooltip("Stop Scan Button")]
        [SerializeField]
        private Button _stopScanButton;
        
        [Tooltip("Share Playback Button")]
        [SerializeField]
        private Button _sharePlaybackButton;
        [SerializeField]
        private Image _loadingIcon;

        private bool _isSharing;

        private ScanStore _scanStore;
        private ScanStore.SavedScan _savedScan;

        void Start()
        {
            _scanStore = _arScanningManager.GetScanStore();
            _arCameraManager.frameReceived += OnCameraFrameReceived;
            
            _sharePlaybackButton.gameObject.SetActive(false);
            _sharePlaybackButton.onClick.AddListener(OnSharedPlaybackButtonClicked);
        }

        private async void OnSharedPlaybackButtonClicked()
        {
            // Do nothing if the button is sharing playback.
            if (_isSharing) return;
            
            _sharePlaybackButton.gameObject.GetComponent<SpriteToggler>().SpriteChange();
            _isSharing = true;
            _loadingIcon.gameObject.SetActive(true);
            _sharePlaybackButton.interactable = false;

            string path = await Task.Run(ZipPlayback);
            if (!string.IsNullOrEmpty(path))
            {
                ShareZippedPlayback(path);
            }

            _sharePlaybackButton.interactable = true;
            _loadingIcon.gameObject.SetActive(false);
            _isSharing = false;
            _sharePlaybackButton.gameObject.GetComponent<SpriteToggler>().SpriteChange();
        }

        private void OnCameraFrameReceived(ARCameraFrameEventArgs args)
        {
            if (args.textures.Count <= 0) 
                return;
            _arCameraManager.frameReceived -= OnCameraFrameReceived;
            InitializeLocation(() =>
            {
                _performScanPanel.SetActive(true);
            });
        }

        private async void InitializeLocation(Action OnInitializeLocationComplete)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                var androidPermissionCallbacks = new PermissionCallbacks();
                androidPermissionCallbacks.PermissionGranted += permissionName =>
                {
                    if (permissionName == "android.permission.ACCESS_FINE_LOCATION")
                    {
                        InitializeLocation(OnInitializeLocationComplete);
                    }
                };

                Permission.RequestUserPermission(Permission.FineLocation, androidPermissionCallbacks);
                return;
            }
#endif
            Input.compass.enabled = true;
            if (Input.location.status != LocationServiceStatus.Stopped)
            {
                return;
            }

            Input.location.Start();
            while (Input.location.status != LocationServiceStatus.Running)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            OnInitializeLocationComplete?.Invoke();
        }

        private void CheckCameraPermission()
        {
#if UNITY_IOS
            if (!_arCameraManager.permissionGranted) {
                HandleCameraPermissionDenied();
                return;
            }
#endif
            HandleCameraPermissionGranted();
        }

        private void HandleCameraPermissionDenied()
        {
#if UNITY_IOS
            _exportScanPanel.SetActive(true);
            _exportScanPanelTitleText.text = "Camera Permission Error";
            _exportScanPanelBodyText.text = "Please check your camera permission in iOS Settings.";
            _startScanButton.gameObject.SetActive(true);
            _stopScanButton.gameObject.SetActive(false);
#endif
        }

        private void HandleCameraPermissionGranted()
        {
            _arScanningManager.ScanRecordingFramerate = (int)_framerateSlider.value;
            _arScanningManager.enabled = true;
        }

        public void StartScanning()
        {
            // Disable Sharing button
            _sharePlaybackButton.gameObject.SetActive(false);

            _maxTimePerChunkSlider.interactable = false;
            _framerateSlider.interactable = false;
            
            _saveScanPanel.SetActive(false);
            CheckCameraPermission();
        }

        public async void StopScanning()
        {
            await _arScanningManager.SaveScan();
            _arScanningManager.enabled = false;
            
            _maxTimePerChunkSlider.interactable = true;
            _framerateSlider.interactable = true;
            
            _saveScanPanel.SetActive(true);
            string scanID = _arScanningManager.GetCurrentScanId();
            _savedScan = _scanStore.GetSavedScans().First(s => s.ScanId == scanID);
        }

        public void DiscardScan()
        {
            _scanStore.DeleteScan(_savedScan);
            _saveScanPanel.SetActive(false);
        }

        public async void StartExporting()
        {
            _performScanPanel.SetActive(false);
            _saveScanPanel.SetActive(false);
            _exportScanPanel.SetActive(true);
            int maxFramesPerChunk = (int)(_maxTimePerChunkSlider.value * _framerateSlider.value);;
            using var exportPayloadBuilder = new ScanArchiveBuilder(_savedScan, new UploadUserInfo(), maxFramesPerChunk);
            _exportScanPanelTitleText.text = "Exporting...";
            string message = string.Empty;
            while (exportPayloadBuilder.HasMoreChunks())
            {
                var exportTask = exportPayloadBuilder.CreateTaskToGetNextChunk();
                exportTask.Start();
                string chunk = await exportTask;
                message += chunk;
                message += "\n";
                _exportScanPanelBodyText.text = message;
            }
            _exportScanPanelTitleText.text = !string.IsNullOrEmpty(message) ? "Exporting Completed" : "Exporting Failed";
            
#if UNITY_IOS && !UNITY_EDITOR
            // Enable Sharing button
            _sharePlaybackButton.gameObject.SetActive(true);
#endif
        }

        private string ZipPlayback()
        {
            if (_savedScan == null)
            {
                Debug.LogWarning("Please create a playback recording first.");
                return null;
            }
            
            string savedScanPath = _savedScan.ScanPath;
            string outputZipFilename = Path.GetFileName(savedScanPath) + ".zip";
            string outputZipFilePath = Path.Join(Path.Join(savedScanPath, ".."), outputZipFilename);
            
            // If file already exists, use it. Otherwise, create a new one.
            if (!File.Exists(outputZipFilePath))
            {
                ZipUtility.CompressFolderToZip(outputZipFilePath, null, savedScanPath);
            }
            
            return outputZipFilePath;
        }

        private void ShareZippedPlayback(string path)
        {
            if (File.Exists(path))
            {
#if UNITY_IOS && !UNITY_EDITOR
                IOSShare.ShareFile(path, "Sharing Playback Recording as zip");
#else
                Debug.LogError("This platform doesn't support sharing");
#endif
            }
            else
            {
                Debug.LogError($"File {path} does not exist");
            }
        }
        
        public void OnFramerateChange()
        {
            _framerateText.text = "Framerate: " + _framerateSlider.value;
        }

        public void OnChunkTimeChange()
        {
            _maxTimePerChunkText.text = "Max Chunk Time: " + _maxTimePerChunkSlider.value + "s";
        }

        private void OnDestroy()
        {
            _sharePlaybackButton.onClick.RemoveListener(OnSharedPlaybackButtonClicked);
        }
    }
}
