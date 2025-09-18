// Copyright 2022-2025 Niantic.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Niantic.Lightship.AR.ObjectDetection;
using Niantic.Lightship.AR.Subsystems.ObjectDetection;
using UnityEngine;
using UnityEngine.UI;

public class ObjectDetectionSample: MonoBehaviour
{
    [SerializeField]
    private float _probabilityThreshold = 0.5f;
    
    [SerializeField] 
    private SliderToggle _filterToggle;
    
    [SerializeField]
    private ARObjectDetectionManager _objectDetectionManager;
    
    private Color[] _colors = new Color[]
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.magenta,
        Color.cyan,
        Color.white,
        Color.black
    };
    
    [SerializeField] [Tooltip("Slider GameObject to set probability threshold")]
    private Slider _probabilityThresholdSlider;

    [SerializeField] [Tooltip("Text to display current slider value")]
    private Text _probabilityThresholdText;

    [SerializeField]
    private Dropdown _categoryDropdown;

    [SerializeField]
    private DrawRect _drawRect;

    private Canvas _canvas;
    [SerializeField] [Tooltip("Categories to display in the dropdown")]
    private List<string> _categoryNames;

    private bool _filterOn = false;

    // The name of the actively selected semantic category
    private string _categoryName = string.Empty;
    private void Awake()
    {
        _canvas = FindFirstObjectByType<Canvas>();


        _probabilityThresholdSlider.value = _probabilityThreshold;
        _probabilityThresholdSlider.onValueChanged.AddListener(OnThresholdChanged);
        OnThresholdChanged(_probabilityThresholdSlider.value);

        _categoryDropdown.onValueChanged.AddListener(categoryDropdown_OnValueChanged);

    }
    
    private void OnMetadataInitialized(ARObjectDetectionModelEventArgs args)
    {
        _objectDetectionManager.ObjectDetectionsUpdated += ObjectDetectionsUpdated;

        // Display person by default.
        _categoryName = _categoryNames[0];
        if (_categoryDropdown is not null && _categoryDropdown.options.Count == 0)
        {
            _categoryDropdown.AddOptions(_categoryNames.ToList());

            var dropdownList = _categoryDropdown.options.Select(option => option.text).ToList();
            _categoryDropdown.value = dropdownList.IndexOf(_categoryName);
        }

    }

    private void ObjectDetectionsUpdated(ARObjectDetectionsUpdatedEventArgs args)
    {
        string resultString = "";
        float _confidence = 0;
        string _name = "";
        var result = args.Results; 
        if (result == null)
        {
            return;
        }
            
        _drawRect.ClearRects();
        for (int i = 0; i < result.Count; i++)
        {
            if(!_filterOn)
            {
                var detection = result[i];
                var categorizations = detection.GetConfidentCategorizations(_probabilityThreshold);
                if (categorizations.Count <= 0)
                {
                    break;
                }
                
                categorizations.Sort((a, b) => b.Confidence.CompareTo(a.Confidence));
                var categoryToDisplay = categorizations[0];
                _confidence = categoryToDisplay.Confidence;
                _name = categoryToDisplay.CategoryName;
            }
            else
            {
                //Get name and confidence of the detected object in a given category.
                _confidence = result[i].GetConfidence(_categoryName);   

                //filter out the objects with confidence less than the threshold 
                if (_confidence< _probabilityThreshold)
                {
                    break;
                }
                _name = _categoryName;
            }
            
            
            int h = Mathf.FloorToInt(_canvas.GetComponent<RectTransform>().rect.height);
            int w = Mathf.FloorToInt(_canvas.GetComponent<RectTransform>().rect.width);

            //Get the rect around the detected object
            var _rect = result[i].CalculateRect(w,h,Screen.orientation);

            resultString = $"{_name}: {_confidence}\n";
            //Draw the Rect.
            _drawRect.CreateRect(_rect, _colors[i % _colors.Length], resultString);

        }
    }
    private void OnThresholdChanged(float newThreshold){
        _probabilityThreshold = newThreshold;
        _probabilityThresholdText.text = "Confidence : " + newThreshold.ToString();
    }
    private void categoryDropdown_OnValueChanged(int val)
    {
        // Update the display category from the dropdown value.
        _categoryName = _categoryDropdown.options[val].text;
    }
    public void Start()
    {
        _objectDetectionManager.enabled = true;
        _objectDetectionManager.MetadataInitialized += OnMetadataInitialized;
        _filterToggle.onValueChanged.AddListener(ToggleFilter);
        _filterOn = _filterToggle.isOn;
        _categoryDropdown.interactable = _filterOn;
    }
    private void ToggleFilter(bool on){
        _filterOn = on;
        _categoryDropdown.interactable = on;
    }
    private void OnDestroy()
    {
        _objectDetectionManager.MetadataInitialized -= OnMetadataInitialized;
        _objectDetectionManager.ObjectDetectionsUpdated -= ObjectDetectionsUpdated;
        if (_probabilityThresholdSlider)
        {
            _probabilityThresholdSlider.onValueChanged.RemoveListener(OnThresholdChanged);
        }
        if (_categoryDropdown is not null)
        {
            _categoryDropdown.onValueChanged.RemoveListener(categoryDropdown_OnValueChanged);
        }
        _filterToggle.onValueChanged.RemoveListener(ToggleFilter);
    }
}