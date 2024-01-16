// Copyright 2022-2024 Niantic.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Niantic.Lightship.AR.Meshing;

public class MeshingFiltering : MonoBehaviour
{
    [SerializeField]
    private LightshipMeshingExtension _meshingExtension;

    private bool _disableMeshFiltering = true;
    private bool _enableAllowList = false;
    private bool _enableBlockList = false;

    // Start is called before the first frame update
    void Start()
    {
        _meshingExtension.AllowList = new List<string>() { "ground" };
        _meshingExtension.BlockList = new List<string>() { "sky", "person", "water", "pet_experimental" };
        DisableMeshFiltering();
    }

    public void ToggleMeshFiltering(){
        _disableMeshFiltering = !_disableMeshFiltering;
    }
    public void ToggleAllowList(){
        _enableAllowList = !_enableAllowList;
    }
    public void ToggleBlockList(){
        _enableBlockList = !_enableBlockList;
    }

    // Update is called once per frame
    public void DisableMeshFiltering()
    {
        _meshingExtension.IsMeshFilteringEnabled = false;
        _meshingExtension.IsFilteringAllowListEnabled = false;
        _meshingExtension.IsFilteringBlockListEnabled = false;
    }

    public void ConfigureMeshFiltering(){
        _meshingExtension.IsMeshFilteringEnabled = !_disableMeshFiltering;
        _meshingExtension.IsFilteringAllowListEnabled = _enableAllowList;
        _meshingExtension.IsFilteringBlockListEnabled = _enableBlockList;
    }
}
