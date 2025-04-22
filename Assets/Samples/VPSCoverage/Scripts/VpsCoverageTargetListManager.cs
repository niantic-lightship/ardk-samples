// Copyright 2022-2025 Niantic.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Niantic.Lightship.AR;
using Niantic.Lightship.AR.VpsCoverage;
using UnityEngine;
using UnityEngine.UI;

public class VpsCoverageTargetListManager : MonoBehaviour
{
    public enum MapApp
    {
        GoogleMaps,
        AppleMaps
    }

    public struct WayspotSelectedArgs
    {
        public string Payload;
        public string Name;
    }
    
    [SerializeField] [Tooltip("Select the app to show directions")]
    private MapApp _mapApp = MapApp.GoogleMaps;

    [Header("ScrollList Setup")]
    [SerializeField]
    [Tooltip("The scroll list holding the items for each target information")]
    private ScrollRect _scrollList;

    [SerializeField] [Tooltip("Template item for target information")]
    private VpsCoverageTargetListItem _itemPrefab;

    [SerializeField]
    private int _maxItemInstances;

    [Header("UI Setup")] [SerializeField] [Tooltip("Button to request to reload the list")]
    private Button _requestButton;

    [SerializeField] [Tooltip("Text to display request status")]
    private Text _requestStatusText;

    [SerializeField] [Tooltip("Slider GameObject to set query radius")]
    private Slider _queryRadiusSlider;

    [SerializeField] [Tooltip("Text to display current slider value")]
    private Text _queryRadiusText;

    [SerializeField] [Tooltip("Toggle GameObject to set location usage")]
    private SliderToggle _useLocationToggle;

    [SerializeField] [Tooltip("Text to display current toggle value")]
    private Text _useLocationText;

    [SerializeField] [Tooltip("Longitude Latitude canvas renderers")]
    private CanvasRenderer[] _longlatCanvasRenderers;

    [SerializeField] [Tooltip("Input field to hold custom longitude")]
    private InputField _latitudeTextField;

    [SerializeField] [Tooltip("Input field to hold custom latitude")]
    private InputField _longitudeTextField;

    [SerializeField]
    private CoverageClientManager _coverageClientManager;

    public event Action<WayspotSelectedArgs> OnWayspotDefaultAnchorButtonPressed;

    private readonly List<VpsCoverageTargetListItem> _targetListItemInstances = new();
    private GameObject _scrollListContent;


    /// <summary>
    /// Setup listeners and callbacks to Change UI and set updated values coverage API Manager.
    /// </summary>
    private void Start()
    {
        _requestButton.interactable = true;
        _scrollListContent = _scrollList.content.gameObject;
#if !UNITY_EDITOR && UNITY_ANDROID
        _mapApp = MapApp.GoogleMaps;
#elif !UNITY_EDITOR && UNITY_IOS
        _mapApp = MapApp.AppleMaps;
#endif
        _queryRadiusSlider.value = _coverageClientManager.QueryRadius;
        _queryRadiusSlider.onValueChanged.AddListener(OnRadiusChanged);
        OnRadiusChanged(_queryRadiusSlider.value);

        _latitudeTextField.text = _coverageClientManager.QueryLatitude.ToString(CultureInfo.CurrentCulture);
        _latitudeTextField.onValueChanged.AddListener(OnLatitudeChanged);
        OnLatitudeChanged(_coverageClientManager.QueryLatitude.ToString(CultureInfo.CurrentCulture));

        _longitudeTextField.text = _coverageClientManager.QueryLongitude.ToString(CultureInfo.CurrentCulture);
        _longitudeTextField.onValueChanged.AddListener(OnLongitudeChanged);
        OnLongitudeChanged(_coverageClientManager.QueryLongitude.ToString(CultureInfo.CurrentCulture));

        _useLocationToggle.isOn = _coverageClientManager.UseCurrentLocation;
        _useLocationToggle.onValueChanged.AddListener(OnUseLocationChanged);
        OnUseLocationChanged(_coverageClientManager.UseCurrentLocation);
    }

    public void RequestAreas()
    {
        _requestStatusText.text = "Requesting coverage from server...";
        _coverageClientManager.TryGetCoverage(OnTryGetCoverage);
        _scrollList.gameObject.SetActive(true);
    }

    public void CloseList()
    {
        ClearListContent();
        _scrollList.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Clears list, Gets result from coverage around the selected location and sorts it to be presentable.
    /// </summary>
    private void OnTryGetCoverage(AreaTargetsResult areaTargetsResult)
    {
        var responseText = string.Empty;

        ClearListContent();

        if (areaTargetsResult.Status == ResponseStatus.Success)
        {
            responseText =
                "Response : " + areaTargetsResult.AreaTargets.Count +
                " targets(s) found within " + areaTargetsResult.QueryRadius +
                "m of [" + areaTargetsResult.QueryLocation.Latitude +
                ", " + areaTargetsResult.QueryLocation.Longitude + "]";

            areaTargetsResult.AreaTargets.Sort((a, b) =>
                a.Area.Centroid.Distance(areaTargetsResult.QueryLocation).CompareTo(
                    b.Area.Centroid.Distance(areaTargetsResult.QueryLocation)));

            var max = _maxItemInstances == 0 ? areaTargetsResult.AreaTargets.Count :
                        Math.Min(_maxItemInstances, areaTargetsResult.AreaTargets.Count);
            for (int i = 0; i < max; ++i)
            {
                var areaTarget = areaTargetsResult.AreaTargets[i];
                // The list object was destroyed, likely by exiting the scene
                if (_scrollListContent == null)
                    return;

                var targetListItemInstance = Instantiate(_itemPrefab, _scrollListContent.transform, false);
                FillTargetItem(targetListItemInstance, areaTargetsResult.QueryLocation, areaTarget.Area,
                    areaTarget.Target);
                _targetListItemInstances.Add(targetListItemInstance);
            }

            var layout = _scrollListContent.GetComponent<VerticalLayoutGroup>();
            var contentTransform = _scrollListContent.GetComponent<RectTransform>();
            float itemHeight = _itemPrefab.GetComponent<RectTransform>().sizeDelta.y;
            contentTransform.sizeDelta = new Vector2
            (
                contentTransform.sizeDelta.x,
                layout.padding.top + _scrollListContent.transform.childCount * (layout.spacing + itemHeight)
            );

            // Scroll all the way up
            contentTransform.anchoredPosition = new Vector2(0, int.MinValue);
        }
        else
        {
            responseText = "Response : " + areaTargetsResult.Status;
        }

        _requestStatusText.text = responseText;
        _requestButton.enabled = true;
    }

    private void OnLatitudeChanged(string newLatitude)
    {
        if (!float.TryParse(_latitudeTextField.text, out float latValue))
        {
            _coverageClientManager.QueryLatitude = 0;
        }
        else
        {
            _coverageClientManager.QueryLatitude = latValue;
        }
    }

    private void OnLongitudeChanged(string newLongitude)
    {
        if (!float.TryParse(_longitudeTextField.text, out float longValue))
        {
            _coverageClientManager.QueryLongitude = 0;
        }
        else
        {
            _coverageClientManager.QueryLongitude = longValue;
        }
    }

    private void OnRadiusChanged(float newRadius)
    {
        var newRadiusAsInt = (int)newRadius;
        _queryRadiusText.text = $"Query Radius : {newRadiusAsInt}";
        _coverageClientManager.QueryRadius = newRadiusAsInt;
    }

    private void OnUseLocationChanged(bool isUsingCurrentLocation)
    {
        _useLocationText.text = "Use current location";

        _latitudeTextField.enabled = !isUsingCurrentLocation;
        _longitudeTextField.enabled = !isUsingCurrentLocation;
        foreach (var canvasRenderer in _longlatCanvasRenderers)
        {
            canvasRenderer.SetAlpha(isUsingCurrentLocation ? .5f : 1f);
        }

        _coverageClientManager.UseCurrentLocation = isUsingCurrentLocation;
    }

    private void ClearListContent()
    {
        foreach (var item in _targetListItemInstances)
        {
            Destroy(item.WayspotImageTexture);
            item.WayspotImageTexture = null;
            Destroy(item.gameObject);
        }

        _targetListItemInstances.Clear();
    }

    private void FillTargetItem
    (
        VpsCoverageTargetListItem coverageTargetListItem,
        LatLng queryLocation,
        CoverageArea area,
        LocalizationTarget target
    )
    {
        coverageTargetListItem.transform.name = target.Name;

        if (area.LocalizabilityQuality == CoverageArea.Localizability.EXPERIMENTAL)
        {
            coverageTargetListItem.transform.Find("WayspotImage").Find("Quality").GetComponent<RawImage>().color = Color.yellow;
            //coverageTargetListItem.BackgroundImageColor = new Color(1, 0.9409157f, 0.6933962f);
        }

        _coverageClientManager.TryGetImageFromUrl(target.ImageURL,
            downLoadedImage => coverageTargetListItem.WayspotImageTexture = downLoadedImage);
        coverageTargetListItem.TitleLabelText = target.Name;

        if (target.Center.Latitude == 0.0 && target.Center.Longitude == 0.0)
        {
            // For private scans without a GPS value, show the distance as unknown.
            coverageTargetListItem.DistanceLabelText += "Distance: ?";
        }
        else
        {
            double distanceInM = target.Center.Distance(queryLocation);
            coverageTargetListItem.DistanceLabelText += "Distance: " + distanceInM.ToString("N0") + " m";
        }

        coverageTargetListItem.SubscribeToNavigateButton(() => { OpenRouteInMapApp(queryLocation, target.Center); });

        coverageTargetListItem.SubscribeToCopyButton(() =>
        {
            OnWayspotDefaultAnchorButtonPressed?.Invoke(new WayspotSelectedArgs{ Payload = target.DefaultAnchor, Name = target.Name });
            GUIUtility.systemCopyBuffer = target.DefaultAnchor;
        });
    }

    private void OpenRouteInMapApp(LatLng from, LatLng to)
    {
        var sb = new StringBuilder();

        if (_mapApp == MapApp.GoogleMaps)
        {
            sb.Append("https://www.google.com/maps/dir/?api=1&origin=");
            sb.Append(from.Latitude);
            sb.Append("+");
            sb.Append(from.Longitude);
            sb.Append("&destination=");
            sb.Append(to.Latitude);
            sb.Append("+");
            sb.Append(to.Longitude);
            sb.Append("&travelmode=walking");
        }
        else if (_mapApp == MapApp.AppleMaps)
        {
            sb.Append("http://maps.apple.com/?saddr=");
            sb.Append(from.Latitude);
            sb.Append("+");
            sb.Append(from.Longitude);
            sb.Append("&daddr=");
            sb.Append(to.Latitude);
            sb.Append("+");
            sb.Append(to.Longitude);
            sb.Append("&dirflg=w");
        }

        Application.OpenURL(sb.ToString());
    }

    private void OnDestroy()
    {
        if (_queryRadiusSlider)
        {
            _queryRadiusSlider.onValueChanged.RemoveListener(OnRadiusChanged);
        }

        if (_latitudeTextField)
        {
            _latitudeTextField.onValueChanged.RemoveListener(OnLatitudeChanged);
        }

        if (_longitudeTextField)
        {
            _longitudeTextField.onValueChanged.RemoveListener(OnLongitudeChanged);
        }

        if (_useLocationToggle)
        {
            _useLocationToggle.onValueChanged.RemoveListener(OnUseLocationChanged);
        }
    }
}
