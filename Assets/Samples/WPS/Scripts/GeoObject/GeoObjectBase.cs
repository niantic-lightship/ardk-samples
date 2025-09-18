using Niantic.Lightship.AR.VpsCoverage;
using Niantic.Lightship.AR.WorldPositioning;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;

public readonly struct GeoObjectSetupParameters
{
    public readonly AreaTarget AreaTarget { get; }
    public readonly GameObject VisibleObjectParent { get; }

    // For WPS GeoObject
    public readonly ARWorldPositioningObjectHelper WpsPositioningHelper { get; }

    // For GPS GeoObject
    public readonly float GpsInitialHeading { get; }

    public GeoObjectSetupParameters(AreaTarget areaTarget, GameObject visibleObjectParent, ARWorldPositioningObjectHelper wpsPositioningHelper, float gpsInitialHeading)
    {
        AreaTarget = areaTarget;
        VisibleObjectParent = visibleObjectParent;
        WpsPositioningHelper = wpsPositioningHelper;
        GpsInitialHeading = gpsInitialHeading;
    }
}

interface IGeoObject
{
    public string Identifier { get; }
    public string Name { get; }
    public string ImageUrl { get; }
    public double Latitude { get; }
    public double Longitude { get; }
    public double Altitude { get; }
    
    public void Setup(GeoObjectSetupParameters parameters);
    public void UpdateData(AreaTarget areaTarget);
    public void UpdateGeoObjectPosition(Transform cameraTransform);
    public void UpdateVisibleObjectsTransform(Transform cameraTransform);
    public void UpdateVisibleObjectsVisibility(Transform cameraTransform, float visibleRange);
    public void TearDown();
}

public abstract class GeoObjectBase : MonoBehaviour, IGeoObject
{
    public string Identifier { get; private set; }
    public string Name { get; private set; }
    public string ImageUrl { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public double Altitude { get; private set; }

    [SerializeField]
    private TextMeshPro _nameLabel;
    
    [SerializeField]
    private TextMeshPro _distanceText;

    [SerializeField]
    private GameObject _labelBackground;

    [SerializeField]
    private GameObject _stick;

    [SerializeField]
    private GameObject _targetInner;
    
    [SerializeField]
    private GameObject _targetOuter;

    [SerializeField]
    protected GameObject _visibleObjects;
    
    [SerializeField]
    protected float _projectionThreshold = 100f; 

    [SerializeField]
    protected float _closeThreshold = 50.0f;
    
    private BoxCollider _boxCollider;
    private Vector3 _boxExtents = Vector3.one;
    
    public abstract void Setup(GeoObjectSetupParameters parameters);
    
    protected void Setup(AreaTarget areaTarget, GameObject visibleObjectParent)
    {
        Identifier = areaTarget.Target.Identifier;
        
        // GeoObject will be positioned by WPS or GPS, but the 'visible objects' will positioned seperately to avoid problems with distant objects
        _visibleObjects.transform.SetParent(visibleObjectParent.transform);
        
        UpdateData(areaTarget);
    }
    
    public void UpdateData(AreaTarget areaTarget)
    {
        Name = areaTarget.Target.Name;
        ImageUrl = areaTarget.Target.ImageURL;
        Latitude = areaTarget.Target.Center.Latitude;
        Longitude = areaTarget.Target.Center.Longitude;
        Altitude = 0;
        
        gameObject.name = $"GeoObject_{Name}";

        if (_nameLabel != null)
        {
            _nameLabel.text = Name;
        }
    }
    
    public abstract void UpdateGeoObjectPosition(Transform cameraTransform);

    public void UpdateVisibleObjectsTransform(Transform cameraTransform)
    {
        // Adjust the orientation of the visible objects to face the camera:
        Vector3 cameraToObject = transform.position - cameraTransform.position;
        cameraToObject.y = 0;
        Quaternion requiredRotation = Quaternion.LookRotation(cameraToObject, Vector3.up);
        _visibleObjects.transform.rotation = requiredRotation;
        
        // Adjust the position of the visible objects to keep them reasonably close to the camera:
        // If distance > _projectionThreshold, project object position so it's _projectionThreshold away
        float distanceMetres = (transform.position - cameraTransform.position).magnitude;
        _visibleObjects.GetComponent<SortDistance>().DisplayDistance = distanceMetres;
        if( distanceMetres > _projectionThreshold )
        {
            Vector3 cameraPosition = cameraTransform.position;
            Vector3 objectPosition = transform.position;
            Vector3 directionVector = (objectPosition - cameraPosition).normalized;

            Vector3 projectedPosition = cameraPosition + (directionVector * _projectionThreshold);
            _visibleObjects.transform.position = projectedPosition;
        }
        else
        {
            // Set the visible object to be the same as the WPS position:
            _visibleObjects.transform.position = transform.position;

            // For close points, force the height to match the camera to get around precision issues
            if(distanceMetres < _closeThreshold)
            {
                Vector3 objectPosition = _visibleObjects.transform.position;
                float closeWeight = 1.0f - distanceMetres / _closeThreshold;
                objectPosition.y = closeWeight * cameraTransform.position.y + (1.0f - closeWeight) *  objectPosition.y;
                _visibleObjects.transform.position = objectPosition;
            }
        }
    }
    
    public void UpdateVisibleObjectsVisibility(Transform cameraTransform, float visibleRange)
    {
        Vector3 distanceDirection = cameraTransform.position - _visibleObjects.transform.position;
        float distance = distanceDirection.magnitude;

        float actualDistance = _visibleObjects.GetComponent<SortDistance>().DisplayDistance;
        _distanceText.text = $"dist: {actualDistance.ToString("F1")}m";

        // Scale the visible objects based on distance
        const float MAX_SCALE_DISTANCE = 40f;
        Vector3 labelScale = Vector3.one;
        if(distance > MAX_SCALE_DISTANCE)
        {
            labelScale = distance / MAX_SCALE_DISTANCE * labelScale;
        }
        _visibleObjects.transform.localScale = labelScale;

        float alpha = 1f;
        
        // Check for overlaps. If there's multiple labels overlapping, fade all but the closest one
        _boxCollider = _visibleObjects.GetComponent<BoxCollider>();
        RectTransform rectTransform = _labelBackground.GetComponent<RectTransform>();
        _boxExtents = new Vector3(rectTransform.rect.width, rectTransform.rect.height, 0.05f );
        _boxCollider.size = _boxExtents * 2f;
        
        Vector3 scaledColliderExtents = _boxExtents.Multiply(labelScale) * 2f;
        Collider[] collisions = Physics.OverlapBox(_labelBackground.transform.position, scaledColliderExtents, _visibleObjects.transform.rotation);
        foreach(Collider hit in collisions)
        {
            if(hit == _boxCollider)
                continue;

            // If the hit object is closer than the current object, fade it out
            SortDistance hitObject = hit.gameObject.GetComponent<SortDistance>();
            if( hitObject?.DisplayDistance < _visibleObjects.GetComponent<SortDistance>().DisplayDistance )
            {
                alpha = 0.5f;
                break;
            }
        }

        Image labelImage = _labelBackground.GetComponent<Image>();

        Color originalColor = new Color(labelImage.color.r, labelImage.color.g, labelImage.color.b, alpha);
        Color adjustedWhite = new Color(1.0f, 1.0f, 1.0f, alpha);
        Color adjustedGrey = new Color(7830189f, 7830189f, 7830189f, alpha);

        labelImage.color = originalColor;
        _stick.GetComponent<Renderer>().material.color = adjustedWhite;
        _targetInner.GetComponent<Renderer>().material.color = adjustedWhite;
        _targetOuter.GetComponent<Renderer>().material.color = originalColor;
        _nameLabel.color = adjustedWhite;
        _distanceText.color = adjustedGrey;
    }
    
    public abstract void TearDown();
    
    private void OnEnable()
    {
        _visibleObjects.SetActive(true);
    }

    private void OnDisable()
    {
        _visibleObjects.SetActive(false);
    }
    
    private void OnDestroy()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        Destroy(_visibleObjects);
    }
}