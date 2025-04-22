// Copyright 2022-2025 Niantic.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RrequestMenuToggler : MonoBehaviour
{
    [SerializeField]
    private AnimationToggle LogState;
    [SerializeField]
    private AnimationToggle HelpState;
    [SerializeField]
    private AnimationToggle SettingsState;

    private bool requested;

    private void Start()
    {
        requested = false;
    }
    public void PastLocalization()
    {
        requested = true;
    }

    public void ToggleState()
    {
        if(!LogState.Open && !HelpState.Open && !requested)
        {
            SettingsState.OpenState();
        }
        else
        {
            SettingsState.CloseState();
        }
    }
}
