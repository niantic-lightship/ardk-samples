// Copyright 2022-2025 Niantic.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectedButton : MonoBehaviour
{

    [SerializeField]
    private SpriteToggler[] ui_buttons;


    public void toggleButtonStates(SpriteToggler pressedButton)
    {
        foreach (var button in ui_buttons)
        {
            var toggler = button.GetComponent<SpriteToggler>();
            if (button != pressedButton )
            {  
                if(toggler.getSelected())
                {
                    toggler.SpriteChange();
                }
            }else
            {
                toggler.SpriteChange();
            }
        }
    }
}
