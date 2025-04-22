// Copyright 2022-2025 Niantic.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//so you can see the edits in edidor as its all shader stuff
//[ExecuteInEditMode]
public class YetiCustomizer : MonoBehaviour
{
    public Color _furColor = new Color(1,0,0);
    public Color _faceColor = new Color (0,1,0);
    public Color _shirtColor  = new Color (0,0,1);
    public Color _waterBottleColor = new Color (1,1,1);
    
    public Color _bagTint = new Color (1,1,1);
    public Color _propsTint = new Color (1,1,1);

    public Vector3 _scale = new Vector3(1,1,1);

    public bool _hasBag=true;
    
    public void ApplySettings()
    {

        transform.localScale = _scale;
 
        var thing = transform.Find("YETI_GEO/Yeti_Body_geo");
        Renderer meshRenderer = thing.GetComponent<Renderer>();

        //fur
        meshRenderer.material.SetColor("_Color", _furColor);

        //face
        meshRenderer.material.SetColor("_Color1", _faceColor);

        //bag
        meshRenderer.material.SetColor("_Color2", _bagTint);

        thing = gameObject.transform.Find("YETI_GEO/Yeti_props_geo");
        meshRenderer = thing.GetComponent<Renderer>();

        //shirt
        meshRenderer.material.SetColor("_Color", _shirtColor);
        meshRenderer.material.SetColor("_Color1", _waterBottleColor);
        meshRenderer.material.SetColor("_Color2", _propsTint);
        
    }

    void Start()
    {
        ApplySettings();
    }

    // Update is called once per frame
    void Update()
    {
        #if UNITY_EDITOR
            ApplySettings();
        #endif
    }
}
