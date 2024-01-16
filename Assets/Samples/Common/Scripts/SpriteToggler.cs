// Copyright 2022-2024 Niantic.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.VectorGraphics;

public class SpriteToggler : MonoBehaviour
{
    [SerializeField]
    private Sprite activeSprite;
    [SerializeField]
    private Sprite inactiveSprite;

    [SerializeField]
    private SVGImage targetButton;

    [SerializeField]
    private AnimationToggle animatorState;

    private bool Selected;

    private void Start()
    {
        Selected = animatorState.Open;
    }
    public void SpriteChange()
    {
        if ( Selected)
        {
            targetButton.sprite = inactiveSprite;
        }
        else
        {
            targetButton.sprite = activeSprite;
        }
        Selected = !Selected;
    }
    public bool getSelected()
    {
        return this.Selected;
    }
}
