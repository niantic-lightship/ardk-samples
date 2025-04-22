// Copyright 2022-2025 Niantic.

using UnityEngine;

namespace WorldPoseSamples
{
    public class CompassMagneticHeading : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image _compassImage;

        [SerializeField] private UnityEngine.UI.Text _coordinatesText;
        
        private void Start()
        {
            if (!Input.location.isEnabledByUser)
            {
                Input.compass.enabled = true;
                Input.location.Start();
            }
        }

        private void Update()
        {
            if (Input.compass.enabled && Input.location.status == LocationServiceStatus.Running)
            {
                float heading = Input.compass.trueHeading;
                _compassImage.rectTransform.rotation = Quaternion.Euler(0, 0, heading);

                _coordinatesText.text = "GPS Latitude: " + Input.location.lastData.latitude + "\nGPS Longitude: " + Input.location.lastData.longitude;
            }
        }
    }
}
