using UnityEngine;

public class GpsGeoObject : GeoObjectBase
{
    private float _initialHeading = 0f;

    public override void Setup(GeoObjectSetupParameters parameters)
    {
        _initialHeading = parameters.GpsInitialHeading;
        Setup(parameters.AreaTarget, parameters.VisibleObjectParent);
    }

    public override void UpdateGeoObjectPosition(Transform cameraTransform)
    {
        if (Input.location.isEnabledByUser && Input.compass.enabled)
        {
            double deviceLatitude = Input.location.lastData.latitude;
            double deviceLongitude = Input.location.lastData.longitude;
            double deviceAltitude = Input.location.lastData.altitude;

            Vector2 eastNorthOffsetMetres = Geographic.EastNorthOffset(Latitude, Longitude, deviceLatitude, deviceLongitude);
            Vector3 trackingOffsetMetres = Quaternion.Euler(0, -_initialHeading, 0) * new Vector3(eastNorthOffsetMetres[0], (float)(Altitude - deviceAltitude), eastNorthOffsetMetres[1]);
            Vector3 trackingMetres = cameraTransform.localPosition + trackingOffsetMetres;
            transform.localPosition = trackingMetres;
        }
    }

    public override void TearDown()
    {
    }
}