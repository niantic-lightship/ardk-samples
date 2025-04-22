// Copyright 2022-2025 Niantic.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationToggle : MonoBehaviour
{
    public bool Open;
    [SerializeField]
    private Animator m_Animator;

    public void toggleStateFunction()
    {
        if (Open)
        {
            m_Animator.SetTrigger("Close");
        }
        else
        {
            m_Animator.SetTrigger("Open");
        }
        Open = !Open;
    }
    public void CloseState()
    {
        if (Open)
        {
            m_Animator.SetTrigger("Close");
            Open = !Open;
        }
        
    }
    public void OpenState()
    {
        if (!Open)
        {
            m_Animator.SetTrigger("Open");
            Open = !Open;
        }
    }
}
