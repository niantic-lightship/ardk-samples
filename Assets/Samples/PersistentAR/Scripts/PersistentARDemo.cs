using System;
using Niantic.ARDK.AR.Protobuf;
using Niantic.Lightship.AR.Subsystems;
using UnityEngine;

namespace Niantic.Lightship.AR.Samples
{
    public class PersistentARDemo : MonoBehaviour
    {
        [Tooltip("The location manager")] [SerializeField]
        private ARLocationManager _arLocationManager;

        [Tooltip("The selector for persistent AR Locations")] [SerializeField]
        private GameObject _arLocationSelector;

        [Tooltip("The template to add a persistent AR location")] [SerializeField]
        private ARLocationSelection _arLocationSelection;

        private void Start()
        {
            if (_arLocationManager.AutoTrack)
            {
                HideARLocationMenu();
            }
            else
            {
                ShowARLocationMenu();
            }
        }

        private void ShowARLocationMenu()
        {
            var arLocations = _arLocationManager.ARLocations;
            foreach (var arLocation in arLocations)
            {
                var arLocationSelection = Instantiate(_arLocationSelection, _arLocationSelection.transform.parent);
                arLocationSelection.Initialize(arLocation);
                arLocationSelection.gameObject.SetActive(true);
                arLocationSelection.ARLocationSelected += HandleLocationSelected;

                void HandleLocationSelected()
                {
                    arLocationSelection.ARLocationSelected -= HandleLocationSelected;
                    _arLocationSelector.SetActive(false);
                    _arLocationManager.SetARLocations(arLocation);
                    _arLocationManager.StartTracking();
                }
            }
        }

        private void HideARLocationMenu()
        {
            _arLocationSelector.SetActive(false);
        }
    }
}
