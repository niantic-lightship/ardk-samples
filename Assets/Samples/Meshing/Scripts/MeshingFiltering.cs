// Copyright 2022-2025 Niantic.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Niantic.Lightship.AR.Meshing;
using UnityEngine.UI;

public class MeshingFiltering : MonoBehaviour
{
    [SerializeField]
    private LightshipMeshingExtension _meshingExtension;


    [SerializeField]
    private Toggle _disableMeshFiltering;
    [SerializeField]
    private Toggle _enableAllowList;
    [SerializeField]
    private Toggle _enableBlockList;

    // Start is called before the first frame update
    void Start()
    {
        _meshingExtension.AllowList = new List<string>() { "ground" };
        _meshingExtension.BlockList = new List<string>() { "sky", "person", "water", "pet_experimental", "loungeable_experimental"};
        DisableMeshFiltering();
    }

    // Update is called once per frame
    public void DisableMeshFiltering()
    {
        _meshingExtension.IsMeshFilteringEnabled = false;
        _meshingExtension.IsFilteringAllowListEnabled = false;
        _meshingExtension.IsFilteringBlockListEnabled = false;
    }

    public void ConfigureMeshFiltering(){
        _meshingExtension.IsMeshFilteringEnabled = !_disableMeshFiltering.isOn;
        _meshingExtension.IsFilteringAllowListEnabled = _enableAllowList.isOn;
        _meshingExtension.IsFilteringBlockListEnabled = _enableBlockList.isOn;
    }
}
