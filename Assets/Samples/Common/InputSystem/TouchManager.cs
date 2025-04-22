// Copyright 2022-2025 Niantic.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.TouchPhase;


public class TouchManager : MonoBehaviour
{
    
    [Serializable]
    public class FloatEvent : UnityEvent<float> { };
    

    [Serializable]
    public class Vector2Event : UnityEvent<Vector2> { };

    [SerializeField] private float _pinchScrollSpeed;
    [SerializeField] private float _mouseScrollSpeed;
    [SerializeField] private float _touchPanSpeed;
    [SerializeField] private float _mousePanSpeed;
    
    private LightshipInput _lightshipInput;
    private bool _isPointerDown;
    private bool IsPointerDown
    {
        
        get { return _isPointerDown; }
        set
        {
            _isPointerDown = value;
            if ((value == false) && (_IsPanActive))
            {
                _IsPanActive = false;
                PanEnded?.Invoke();
            }

        }
    }
    private bool _IsPanActive = false;
    private Dictionary<int, TouchState> _activeTouches = new Dictionary<int, TouchState>();
    
    
    
    private float _lastPinchDistance = 0f;
    private bool _resetPinchDistance = false;
    private Coroutine activePinchCoroutine = null;
    
    [SerializeField] public FloatEvent PinchEvent = new FloatEvent();
    [SerializeField] public Vector2Event PanEvent = new Vector2Event();
    [SerializeField] public UnityEvent PanEnded = new UnityEvent();
    [SerializeField] public Vector2Event TouchUpEvent = new Vector2Event();

    
    private void Awake()
    {
        _lightshipInput = new LightshipInput();


    }

    private void OnEnable()
    {
        _lightshipInput.Enable();
    }

    private void OnDisable()
    {
        _lightshipInput.Disable();
    }

    private void Start()
    {
        _lightshipInput.Input.PrimaryTouch.performed += ctx => PrimaryTouchCallback(ctx);
        _lightshipInput.Input.PrimaryTouch.started += ctx => PrimaryTouchCallback(ctx);
        _lightshipInput.Input.PrimaryTouch.canceled += ctx => PrimaryTouchCallback(ctx);

        _lightshipInput.Input.SecondaryTouch.performed += ctx => SecondaryTouchCallback(ctx);
        _lightshipInput.Input.SecondaryTouch.started += ctx => SecondaryTouchCallback(ctx);
        _lightshipInput.Input.SecondaryTouch.canceled += ctx => SecondaryTouchCallback(ctx);

        _lightshipInput.Input.MouseScroll.performed += ctx => MouseScrollCallback(ctx);
        _lightshipInput.Input.MousePan.performed += ctx => MousePanCallback(ctx);
        _lightshipInput.Input.MousePan.canceled += ctx => MousePanCallback(ctx);

        _lightshipInput.Input.MouseHold.started += ctx => MouseHoldCallback(ctx);
        _lightshipInput.Input.MouseHold.performed += ctx => MouseHoldCallback(ctx);
        _lightshipInput.Input.MouseHold.canceled += ctx => MouseHoldCallback(ctx);

    }

    private void MouseHoldCallback(InputAction.CallbackContext ctx)
    {
        switch (ctx.phase)
        {
            case InputActionPhase.Started :
                IsPointerDown = true;
                break;
            case InputActionPhase.Canceled :
                IsPointerDown = false;
                if (!_IsPanActive)
                {
                    TouchUpEvent?.Invoke(Mouse.current.position.ReadValue());
                }
                break;
            
        }
        
    }
    

    private void MousePanCallback(InputAction.CallbackContext ctx)
    {
        var mousepan = ctx.ReadValue<Vector2>();
        if (ctx.phase == InputActionPhase.Performed)
        {
            _IsPanActive = true;
        }
        ProcessPan(mousepan  * _mousePanSpeed);
    }

    private void MouseScrollCallback(InputAction.CallbackContext ctx)
    {
        
        var mousecroll = ctx.ReadValue<Vector2>();
        ProcessScrollZoom(mousecroll);
        
        
    }

    private void SecondaryTouchCallback(InputAction.CallbackContext ctx)
    {
        var touch = ctx.ReadValue<TouchState>();
        PreprocessTouch(touch);
        
    }

    private void PrimaryTouchCallback(InputAction.CallbackContext ctx)
    {
        var touch = ctx.ReadValue<TouchState>();
        PreprocessTouch(touch);
        
    }

    private void PreprocessTouch(TouchState touch)
    {
        
        switch (touch.phase)
        {
            case UnityEngine.InputSystem.TouchPhase.Began :
                
                _activeTouches.TryAdd(touch.touchId, touch);
                break;
            case UnityEngine.InputSystem.TouchPhase.Ended :
                _activeTouches.Remove(touch.touchId);
                break;
            case UnityEngine.InputSystem.TouchPhase.Canceled :
                _activeTouches.Remove(touch.touchId);
                break;
            case UnityEngine.InputSystem.TouchPhase.None :
                break;
            default :
                _activeTouches[touch.touchId] = touch;
                break;
        }
        
        if (_activeTouches.Count > 1)
        {
            if (activePinchCoroutine == null)
            {
                activePinchCoroutine = StartCoroutine(ProcessPinch());
            }
        } 
        else
        {
            if (activePinchCoroutine != null)
            {
                StopCoroutine(activePinchCoroutine);
                activePinchCoroutine = null;
            }

            if (_activeTouches.Count == 1)
            {
                ProcessTouch();
            }
            else
            {
                IsPointerDown = false;
            }
        }
        
        if (IsPointerDown == false) 
            if (_IsPanActive)
            {
                _IsPanActive = false;
                PanEnded?.Invoke();
            }
            else
            {
                TouchUpEvent?.Invoke(touch.position);
            } 

        
    }



    #region Process Gestures

    private void ProcessPan(Vector2 delta)
    {
        
        PanEvent?.Invoke(delta);
    }
    
    

    private IEnumerator ProcessPinch()
    {
        _resetPinchDistance = true;
        while (true)
        {

            yield return null;
            
            var orderedTouches = _activeTouches.OrderBy(x => x.Key).ToArray();
            
            var touchOne = orderedTouches[0];
            var touchTwo = orderedTouches[1];

            var pinchDistance = Vector2.Distance(touchOne.Value.position, touchTwo.Value.position);
            if (_resetPinchDistance)
            {
                _lastPinchDistance = pinchDistance;
                _resetPinchDistance = false;
            }
            
            float pinchDelta = (pinchDistance - _lastPinchDistance);
            _lastPinchDistance = pinchDistance;
            

            PinchEvent?.Invoke(pinchDelta  * _pinchScrollSpeed);
        }
    }

    private void ProcessScrollZoom(Vector2 MouseScrollDelta)
    {
        var yScrollDelta = MouseScrollDelta.y * _mouseScrollSpeed;
        PinchEvent?.Invoke(yScrollDelta);
    }

    private void ProcessMouseMap()
    {
        
    }

    private void ProcessTouch()
    {
        var orderedTouches = _activeTouches.OrderBy(x => x.Key).ToArray();
        var touch = orderedTouches[0].Value;
        switch (touch.phase)
        {
            case UnityEngine.InputSystem.TouchPhase.Began :
                IsPointerDown = true;
                break;
            case UnityEngine.InputSystem.TouchPhase.Ended :
                break;
            case UnityEngine.InputSystem.TouchPhase.Moved:
                _IsPanActive = true;
                var delta = touch.delta;
                if (delta.x + delta.y != 0)
                {
                    ProcessPan(delta);
                }
                

                break;
        }
    }
    
    
    #endregion
    
    




}
