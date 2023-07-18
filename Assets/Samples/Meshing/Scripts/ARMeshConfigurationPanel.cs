using System;
using System.Collections;
using System.Collections.Generic;
using Niantic.Lightship.AR.ARFoundation;
using UnityEngine;
using UnityEngine.UI;

public class ARMeshConfigurationPanel : MonoBehaviour
{

    [SerializeField] private GameObject _settingsPanel;

    private NianticLightshipMeshingExtensionSettings _meshingSettings;
    private InputField _frameRateValue;
    private InputField _integrationDistanceValue;
    private InputField _voxelSize;
    private Toggle _enableDistanceBasedVolumetricCleanup;
    private InputField _blockSize;
    private InputField _cullingDistance;
    private Toggle _enableMeshDecimation;

    public void TogglePanel()
    {
        bool isActive = _settingsPanel.activeSelf;
        _settingsPanel.SetActive(!isActive);
    }

    public void Start()
    {
        _meshingSettings = FindObjectOfType<NianticLightshipMeshingExtensionSettings>();

        _frameRateValue = GameObject.Find("Frame Rate Value").GetComponent<InputField>();
        _frameRateValue.text = _meshingSettings._frameRate.ToString();

        _integrationDistanceValue = GameObject.Find("Integration Distance Value").GetComponent<InputField>();
        _integrationDistanceValue.text = _meshingSettings._maximumIntegrationDistance.ToString();

        _voxelSize = GameObject.Find("Voxel Size Value").GetComponent<InputField>();
        _voxelSize.text = _meshingSettings._voxelSize.ToString();

        _enableDistanceBasedVolumetricCleanup = GameObject.Find("Distance Based Volumetric Cleanup Config").GetComponent<Toggle>();
        _enableDistanceBasedVolumetricCleanup.isOn = _meshingSettings._enableDistanceBasedVolumetricCleanup;

        _blockSize = GameObject.Find("Block Size Value").GetComponent<InputField>();
        _blockSize.text = _meshingSettings._meshBlockSize.ToString();

        _cullingDistance = GameObject.Find("Culling Distance Value").GetComponent<InputField>();
        _cullingDistance.text = _meshingSettings._meshCullingDistance.ToString();

        _enableMeshDecimation = GameObject.Find("Mesh Decimation Config").GetComponent<Toggle>();
        _enableMeshDecimation.isOn = _meshingSettings._enableMeshDecimation;
    }

    public void Configure()
    {
        _meshingSettings._frameRate = int.Parse(_frameRateValue.text);
        _meshingSettings._maximumIntegrationDistance = float.Parse(_integrationDistanceValue.text);
        _meshingSettings._voxelSize = float.Parse(_voxelSize.text);
        _meshingSettings._enableDistanceBasedVolumetricCleanup = _enableDistanceBasedVolumetricCleanup.isOn;
        _meshingSettings._meshBlockSize = float.Parse(_blockSize.text);
        _meshingSettings._meshCullingDistance = float.Parse(_cullingDistance.text);
        _meshingSettings._enableMeshDecimation = _enableMeshDecimation.isOn;
        _meshingSettings.Configure();
    }
}
