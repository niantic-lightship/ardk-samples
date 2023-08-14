using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Niantic.Lightship.AR.Extensions.Gameboard;
using UnityEngine.InputSystem;

/// <summary>
/// GameboardSample this sample shows how to quickly used gameboard to add user driven point and click navigation
/// when you first touch the screen it will place your agent prefab
/// then if you tap again the agent will walk to that location
/// there is a toggle button to show hide the gameboard and path.
/// It assumes the _agentPrefab has GameboardAgent on it.
/// If you have written your own agent type you would swap for that or inherit from it so you can leverage polymophism.
/// </summary>
public class GameboardDevSample : MonoBehaviour
{
    [SerializeField]
    private Camera _camera;

    [SerializeField]
    private GameboardManager _gameboardManager;

    [SerializeField]
    private GameObject _agentPrefab;

    [SerializeField]
    private GameObject _Visualization;

    private GameObject _creature;
    private GameboardAgent _agent;

    private PlayerInput lightshipInput;
    private InputAction primaryTouch;

    private void Awake(){
        //Get the input actions.
        lightshipInput = GetComponent<PlayerInput>();
        primaryTouch = lightshipInput.actions["Point"];
    }
    void Update()
    {
        HandleTouch();
    }

    public void ToggleVisualisation()
    {
        if(_creature == null ){
            //turn off the rendering for the gamebaard
            _gameboardManager.GetComponent<GameboardRenderer>().enabled =
                !_gameboardManager.GetComponent<GameboardRenderer>().enabled;

            //turn off the path rendering on any agent
            _agent.GetComponent<GameboardAgentPathRenderer>().enabled =
                !_agent.GetComponent<GameboardAgentPathRenderer>().enabled;
        }
    }

    private void HandleTouch()
    {
        //Get the primaryTouch from our input actions.
        if (!primaryTouch.WasPerformedThisFrame())
            return;
        else{
            //project the touch point from screen space into 3d and pass that to your agent as a destination
            Ray ray = _camera.ScreenPointToRay(primaryTouch.ReadValue<Vector2>());
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (_creature == null )
                {
                    //TODO: Add the is there enough space to place.
                    //have a nice fits/dont fit in the space.

                    _creature = Instantiate(_agentPrefab);
                    _creature.transform.position = hit.point;
                    _agent = _creature.GetComponent<GameboardAgent>();
                    _Visualization.SetActive(true);

                }
                else
                {
                    _agent.SetDestination(hit.point);
                }
            }
        }
    }
}
