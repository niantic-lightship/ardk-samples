// Copyright 2022-2025 Niantic.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RequestMenuToggler : MonoBehaviour
{
    [SerializeField]
    private AnimationToggle LogState;
    [SerializeField]
    private AnimationToggle HelpState;
    [SerializeField]
    private AnimationToggle SettingsState;

    private bool _requested;
    
    public void PreLocalization()
    {
        _requested = false;
    }
    
    public void PastLocalization()
    {
        _requested = true;
    }

    public void ToggleState()
    {
        if(!LogState.Open && !HelpState.Open && !_requested)
        {
            SettingsState.OpenState();
        }
        else
        {
            SettingsState.CloseState();
        }
    }
}
