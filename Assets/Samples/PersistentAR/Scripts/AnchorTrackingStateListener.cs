// Copyright 2022-2025 Niantic.
using Niantic.Lightship.AR.Loader;
using Niantic.Lightship.AR.LocationAR;
using Niantic.Lightship.AR.PersistentAnchors;
using UnityEngine;
using UnityEngine.UI;

namespace Niantic.Lightship.AR.Samples
{
    public class AnchorTrackingStateListener : MonoBehaviour
    {
        [SerializeField]
        private ARLocationManager _arLocationManager;

        [SerializeField]
        private Text _AnchorTrackingStateText;

        private void OnEnable()
        {
            _arLocationManager.locationTrackingStateChanged += OnLocationTrackingStateChanged;
        }

        void Start()
        {
            if (string.IsNullOrWhiteSpace(LightshipSettings.Instance.ApiKey))
            {
                if (_AnchorTrackingStateText != null)
                {
                    _AnchorTrackingStateText.text = "No API key is set";
                }

                return;
            }

            if (_arLocationManager == null)
            {
                if (_AnchorTrackingStateText != null)
                {
                    _AnchorTrackingStateText.text = "No Location Manager to listen to";
                }

                return;
            }
            if (_arLocationManager.ARLocations.Length <1)
            {
                _AnchorTrackingStateText.text = "Add an AR Location to the AR Location Manager.";
                return;
            }

            if (_AnchorTrackingStateText != null)
            {
                _AnchorTrackingStateText.text = "Select a Location to start tracking";
            }
        }

        private void OnDisable()
        {
            _arLocationManager.locationTrackingStateChanged -= OnLocationTrackingStateChanged;
        }

        private void OnLocationTrackingStateChanged(ARLocationTrackedEventArgs args)
        {
            if (args.Tracking)
            {
                if (_AnchorTrackingStateText != null)
                {
                    _AnchorTrackingStateText.text = $"Anchor Tracked";
                }
            }
            else
            {
                if (_AnchorTrackingStateText != null)
                {
                    _AnchorTrackingStateText.text = $"Anchor Untracked";
                }
            }
        }
    }
}
