// Copyright 2022-2025 Niantic.
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class YetMotion : MonoBehaviour
{
    Vector2 _lastPos = new Vector2(0, 0);
    Animator _animator;

    // Start is called before the first frame update
    void Start()
    {
        //get my animator so i can set variables in the anim state machine.
        _animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        //am i moving, if so tell the animation controller that.
        //only handles walking and idle.
        Vector2 pos = new Vector2(gameObject.transform.position.x, gameObject.transform.position.z);
        float dist = (pos - _lastPos).magnitude;
        _lastPos = pos;

        float speed = dist / Time.deltaTime;
        _animator.SetFloat("speed", speed);

    }
}
