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
        _frameRateValue.text = _meshingSettings.TargetFrameRate.ToString();

        _integrationDistanceValue = GameObject.Find("Integration Distance Value").GetComponent<InputField>();
        _integrationDistanceValue.text = _meshingSettings.MaximumIntegrationDistance.ToString();

        _voxelSize = GameObject.Find("Voxel Size Value").GetComponent<InputField>();
        _voxelSize.text = _meshingSettings.VoxelSize.ToString();

        _enableDistanceBasedVolumetricCleanup = GameObject.Find("Distance Based Volumetric Cleanup Config").GetComponent<Toggle>();
        _enableDistanceBasedVolumetricCleanup.isOn = _meshingSettings.EnableDistanceBasedVolumetricCleanup;

        _blockSize = GameObject.Find("Block Size Value").GetComponent<InputField>();
        _blockSize.text = _meshingSettings.MeshBlockSize.ToString();

        _cullingDistance = GameObject.Find("Culling Distance Value").GetComponent<InputField>();
        _cullingDistance.text = _meshingSettings.MeshCullingDistance.ToString();

        _enableMeshDecimation = GameObject.Find("Mesh Decimation Config").GetComponent<Toggle>();
        _enableMeshDecimation.isOn = _meshingSettings.EnableMeshDecimation;
    }

    public void Configure()
    {
        _meshingSettings.TargetFrameRate = int.Parse(_frameRateValue.text);
        _meshingSettings.MaximumIntegrationDistance = float.Parse(_integrationDistanceValue.text);
        _meshingSettings.VoxelSize = float.Parse(_voxelSize.text);
        _meshingSettings.EnableDistanceBasedVolumetricCleanup = _enableDistanceBasedVolumetricCleanup.isOn;
        _meshingSettings.MeshBlockSize = float.Parse(_blockSize.text);
        _meshingSettings.MeshCullingDistance = float.Parse(_cullingDistance.text);
        _meshingSettings.EnableMeshDecimation = _enableMeshDecimation.isOn;
        _meshingSettings.Configure();
    }
}
