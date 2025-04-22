// Copyright 2022-2025 Niantic.
using System.Collections;
using System.Collections.Generic;
using Niantic.Lightship.AR.NavigationMesh;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

/// <summary>
/// This sample shows how to quickly used Niantic's NavMesh to add user driven point and click navigation
/// when you first touch the screen it will place your agent prefab
/// then if you tap again the agent will walk to that location
/// there is a toggle button to show hide the navigation mesh and path.
/// It assumes the _agentPrefab has LightshipNavMeshAgent on it.
/// You can overload it if you want to.
/// </summary>
public class LightshipNavMeshDevSample : MonoBehaviour
{
    [SerializeField]
    private Camera _camera;

    [FormerlySerializedAs("_gameboardManager")] [SerializeField]
    private LightshipNavMeshManager _navMeshManager;

    [FormerlySerializedAs("_agentPrefab")] [SerializeField]
    private GameObject agentPrefab;

    [FormerlySerializedAs("_Visualization")] [SerializeField]
    private GameObject visualization;

    private GameObject _creature;
    private LightshipNavMeshAgent _agent;

    private PlayerInput _lightshipInput;
    private InputAction _primaryTouch;

    private void Awake(){
        //Get the input actions.
        _lightshipInput = GetComponent<PlayerInput>();
        _primaryTouch = _lightshipInput.actions["Point"];
    }

    void Update()
    {
        HandleTouch();
    }

    public void ToggleVisualisation()
    {
        if(_creature != null ){
            //turn off the rendering for the nav mesh
            _navMeshManager.GetComponent<LightshipNavMeshRenderer>().enabled =
                !_navMeshManager.GetComponent<LightshipNavMeshRenderer>().enabled;

            //turn off the path rendering on any agent
            _agent.GetComponent<LightshipNavMeshAgentPathRenderer>().enabled =
                !_agent.GetComponent<LightshipNavMeshAgentPathRenderer>().enabled;
        }
    }

    private void HandleTouch()
    {
        //Get the primaryTouch from our input actions.
        if (!_primaryTouch.WasPerformedThisFrame())
            return;
        else{
            //project the touch point from screen space into 3d and pass that to your agent as a destination
            Ray ray = _camera.ScreenPointToRay(_primaryTouch.ReadValue<Vector2>());
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit) &&
                _navMeshManager.LightshipNavMesh.IsOnNavMesh(hit.point, 0.2f) &&
                !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                if (_creature == null )
                {
                    //TODO: Add the is there enough space to place.
                    //have a nice fits/dont fit in the space.

                    _creature = Instantiate(agentPrefab);
                    _creature.transform.position = hit.point;
                    _agent = _creature.GetComponent<LightshipNavMeshAgent>();
                    visualization.SetActive(true);

                }
                else
                {
                    _agent.SetDestination(hit.point);
                }
            }
        }
    }

}
