// Copyright 2022-2025 Niantic.
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Niantic.ARDK.AR.Scanning;
using Niantic.Lightship.AR.Scanning;
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
        
        [Tooltip("Framerate at which the scan is recorded")]
        [Range(1, 30)]
        [SerializeField]
        private int _scanRecordingFramerate = 30;
        
        [Tooltip("Max recording time (sec) per one chunk")]
        [Range(30, 600)]
        [SerializeField]
        private float _maxTimePerChunk = 600;
        
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

        private ScanStore _scanStore;
        private ScanStore.SavedScan _savedScan;

        void Start()
        {
            _scanStore = _arScanningManager.GetScanStore();
            _arCameraManager.frameReceived += OnCameraFrameReceived;
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
            _arScanningManager.ScanRecordingFramerate = _scanRecordingFramerate;
            _arScanningManager.enabled = true;
        }

        public void StartScanning()
        {
            _saveScanPanel.SetActive(false);
            CheckCameraPermission();
        }

        public async void StopScanning()
        {
             await _arScanningManager.SaveScan();
            _arScanningManager.enabled = false;
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
            int maxFramesPerChunk = (int)(_maxTimePerChunk * _scanRecordingFramerate);
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
        }
    }
}
