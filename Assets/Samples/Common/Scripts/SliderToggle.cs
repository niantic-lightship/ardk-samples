// Copyright 2022-2025 Niantic.
using System;
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
        var Xvalue = on ? Math.Abs(_handlePosition.x) : Math.Abs(_handlePosition.x) * -1;
        _handlePosition = new Vector2(Xvalue, _handlePosition.y);

        uiHandleRectTransform.anchoredPosition =_handlePosition;
    }
    protected override void OnDestroy()
    {
        onValueChanged.RemoveListener(OnSwitch);
        base.OnDestroy();
    }
}
