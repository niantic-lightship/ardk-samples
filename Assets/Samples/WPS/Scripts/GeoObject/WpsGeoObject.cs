using Niantic.Lightship.AR.VpsCoverage;
using Niantic.Lightship.AR.WorldPositioning;
using Unity.VisualScripting;
using UnityEngine;

public class WpsGeoObject : GeoObjectBase
{
    private ARWorldPositioningObjectHelper _positioningHelper;

    public override void Setup(GeoObjectSetupParameters parameters)
    {
        _positioningHelper = parameters.WpsPositioningHelper;
        Setup(parameters.AreaTarget, parameters.VisibleObjectParent);
    }

    public override void UpdateGeoObjectPosition(Transform cameraTransform)
    {
        _positioningHelper.AddOrUpdateObject(gameObject,
            Latitude,
            Longitude,
            Altitude,
            Quaternion.identity);
    }

    public override void TearDown()
    {
        _positioningHelper.RemoveObject(gameObject);
    }
}