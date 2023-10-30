// Copyright 2023 Niantic, Inc. All Rights Reserved.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderToggle : Toggle
{
    [SerializeField] private RectTransform uiHandleRectTransform;

    private Vector2 _handlePosition;

    protected override void Awake()
    {
        _handlePosition = uiHandleRectTransform.anchoredPosition;

        onValueChanged.AddListener(OnSwitch);
        if (isOn)
        {
            OnSwitch(true);
        }
        
        base.Awake();
    }

    private void OnSwitch(bool on)
    {
        uiHandleRectTransform.anchoredPosition =on? _handlePosition * -1: _handlePosition;
    }
    protected override void OnDestroy()
    {
        onValueChanged.RemoveListener(OnSwitch);
        base.OnDestroy();
    }
}
