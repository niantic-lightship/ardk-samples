// Copyright 2022-2025 Niantic.
using System;
using UnityEngine;
using UnityEngine.UI;

public class VpsCoverageTargetListItem : MonoBehaviour
{
    [SerializeField]
    private RawImage wayspotImage;

    [SerializeField]
    private Text titleLabel;

    [SerializeField]
    private Text distanceLabel;

    [SerializeField]
    private Button navigateButton;

    [SerializeField]
    private Button copyButton;

    [SerializeField]
    private Image backgroundImage;

    public string TitleLabelText
    {
        get => titleLabel.text;
        set => titleLabel.text = value;
    }

    public string DistanceLabelText
    {
        get => distanceLabel.text;
        set => distanceLabel.text = value;
    }

    public Texture WayspotImageTexture
    {
        get => wayspotImage.texture;
        set => wayspotImage.texture = value;
    }

    public Color BackgroundImageColor
    {
        get => backgroundImage.color;
        set => backgroundImage.color = value;
    }

    public void SubscribeToNavigateButton(Action action)
    {
        navigateButton.onClick.AddListener(action.Invoke);
    }

    public void SubscribeToCopyButton(Action action)
    {
        copyButton.onClick.AddListener(action.Invoke);
    }
}
