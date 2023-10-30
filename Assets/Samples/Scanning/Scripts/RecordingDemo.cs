// Copyright 2023 Niantic, Inc. All Rights Reserved.
using System;
using System.Linq;
using System.Threading.Tasks;
using Niantic.ARDK.AR.Scanning;
using Niantic.Lightship.AR.Scanning;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace Niantic.Lightship.AR.Samples
{
    public class RecordingDemo : MonoBehaviour
    {
        [Tooltip("The manager used to perform the scanning")]
        [SerializeField]
        private ARScanningManager _arScanningManager;
        
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

        public void StartScanning()
        {
            _saveScanPanel.SetActive(false);
            _arScanningManager.enabled = true;
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
            using var exportPayloadBuilder = new ScanArchiveBuilder(_savedScan, new UploadUserInfo());
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
