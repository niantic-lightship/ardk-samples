// Copyright 2022-2025 Niantic.

using Niantic.Lightship.AR.Meshing;
using UnityEngine;
using UnityEngine.UI;

public class ARMeshConfigurationPanel : MonoBehaviour
{

    [SerializeField] private GameObject _settingsPanel;

    private LightshipMeshingExtension _lightshipMeshingExtension;
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
        _lightshipMeshingExtension = FindFirstObjectByType<LightshipMeshingExtension>();

        _frameRateValue = GameObject.Find("Frame Rate Value").GetComponent<InputField>();
        _frameRateValue.text = _lightshipMeshingExtension.TargetFrameRate.ToString();

        _integrationDistanceValue = GameObject.Find("Integration Distance Value").GetComponent<InputField>();
        _integrationDistanceValue.text = _lightshipMeshingExtension.MaximumIntegrationDistance.ToString();

        _voxelSize = GameObject.Find("Voxel Size Value").GetComponent<InputField>();
        _voxelSize.text = _lightshipMeshingExtension.VoxelSize.ToString();

        _enableDistanceBasedVolumetricCleanup = GameObject.Find("Distance Based Volumetric Cleanup Config").GetComponent<SliderToggle>();
        _enableDistanceBasedVolumetricCleanup.isOn = _lightshipMeshingExtension.EnableDistanceBasedVolumetricCleanup;

        _blockSize = GameObject.Find("Block Size Value").GetComponent<InputField>();
        _blockSize.text = _lightshipMeshingExtension.MeshBlockSize.ToString();

        _cullingDistance = GameObject.Find("Culling Distance Value").GetComponent<InputField>();
        _cullingDistance.text = _lightshipMeshingExtension.MeshCullingDistance.ToString();

        _enableMeshDecimation = GameObject.Find("Mesh Decimation Config").GetComponent<SliderToggle>();
        _enableMeshDecimation.isOn = _lightshipMeshingExtension.EnableMeshDecimation;
    }

    public void Configure()
    {
        _lightshipMeshingExtension.TargetFrameRate = int.Parse(_frameRateValue.text);
        _lightshipMeshingExtension.MaximumIntegrationDistance = float.Parse(_integrationDistanceValue.text);
        _lightshipMeshingExtension.VoxelSize = float.Parse(_voxelSize.text);
        _lightshipMeshingExtension.EnableDistanceBasedVolumetricCleanup = _enableDistanceBasedVolumetricCleanup.isOn;
        _lightshipMeshingExtension.MeshBlockSize = float.Parse(_blockSize.text);
        _lightshipMeshingExtension.MeshCullingDistance = float.Parse(_cullingDistance.text);
        _lightshipMeshingExtension.EnableMeshDecimation = _enableMeshDecimation.isOn;
        _lightshipMeshingExtension.Configure();
    }
}
