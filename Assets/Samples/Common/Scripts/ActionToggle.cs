// Copyright 2022-2025 Niantic.
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

/// <summary>
/// Facilitates running two arbitrary logic options 
/// </summary>
public class ActionToggle : MonoBehaviour
{
    [SerializeField] private UnityEvent TrueAction;

    [SerializeField] private UnityEvent FalseAction;

    [SerializeField] private bool IsOn = false;

    private void Awake()
    {
        Assert.IsNotNull(TrueAction);
        Assert.IsNotNull(FalseAction);
    }

    private void Start()
    {
        InvokeAction();
    }

    private void InvokeAction()
    {
        (IsOn ? TrueAction : FalseAction).Invoke();
    }

    public void Toggle()
    {
        IsOn = !IsOn;
        InvokeAction();
    }
}