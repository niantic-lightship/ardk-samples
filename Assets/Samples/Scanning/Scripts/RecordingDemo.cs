using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Niantic.ARDK.AR.Scanning;
using Niantic.Lightship.AR.ARFoundation.Scanning;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;

namespace Niantic.Lightship.AR.Samples
{
    public class RecordingDemo : MonoBehaviour
    {
        [Tooltip("The manager used to perform the scanning")]
        [SerializeField]
        private ARScanningManager _arScanningManager;

        [Tooltip("Scan Panel")]
        [SerializeField]
        private GameObject _scanPanel;

        [Tooltip("Export Panel")]
        [SerializeField]
        private ExportScanPanel _exportScanPanel;

        [Tooltip("Button to start scanning")]
        [SerializeField]
        private Button _startScanningButton;

        [Tooltip("Button to stop scanning")]
        [SerializeField]
        private Button _stopScanningButton;

        [Tooltip("Button to export a scan")]
        [SerializeField]
        private Button _startExportButton;


        private ScanStore _scanStore;
        private ScanStore.SavedScan _savedScan;

        void Start()
        {
            _scanStore = _arScanningManager.GetScanStore();
            InitializeLocation();
        }

        private async void InitializeLocation()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                var androidPermissionCallbacks = new PermissionCallbacks();
                androidPermissionCallbacks.PermissionGranted += permissionName =>
                {
                    if (permissionName == "android.permission.ACCESS_FINE_LOCATION")
                    {
                        InitializeLocation();
                    }
                };

                Permission.RequestUserPermission(Permission.FineLocation, androidPermissionCallbacks);
                return;
            }
#endif
            Input.compass.enabled = true;
            if (Input.location.status == LocationServiceStatus.Stopped)
            {
                Input.location.Start();
                while (Input.location.status != LocationServiceStatus.Running)
                {
                    await Task.Delay(100); // ms
                }
            }
        }

        public void StartScanning()
        {
            _stopScanningButton.gameObject.SetActive(true);
            _startExportButton.gameObject.SetActive(false);
            _startScanningButton.gameObject.SetActive(false);
            _arScanningManager.enabled = true;
        }

        public async void StopScanning()
        {
            _stopScanningButton.gameObject.SetActive(false);
             await _arScanningManager.SaveScan();
            _arScanningManager.enabled = false;
            _startExportButton.gameObject.SetActive(true);
            string scanID = _arScanningManager.GetCurrentScanId();
            _savedScan = _scanStore.GetSavedScans().First(s => s.ScanId == scanID);

        }

        public async void StartExporting()
        {
            _scanPanel.SetActive(false);
            _exportScanPanel.gameObject.SetActive(true);
            using var exportPayloadBuilder = new ScanArchiveBuilder(_savedScan, new UploadUserInfo());
            _exportScanPanel.SetExportStatusText(true, "");
            string message = "";
            while (exportPayloadBuilder.HasMoreChunks())
            {
                var exportTask = exportPayloadBuilder.CreateTaskToGetNextChunk();
                exportTask.Start();
                string chunk = await exportTask;
                message += chunk;
                message += "\n";
                _exportScanPanel.SetExportStatusText(true, message);
            }
            _exportScanPanel.SetExportStatusText(false, message);
        }
    }
}
