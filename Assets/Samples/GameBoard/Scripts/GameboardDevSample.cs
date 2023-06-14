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

    private GameObject _creature;
    private GameboardAgent _agent;

    void Update()
    {
        HandleTouch();
    }

    public void ToggleVisualisation()
    {
        //turn off the rendering for the gamebaard
        _gameboardManager.GetComponent<GameboardRenderer>().enabled =
            !_gameboardManager.GetComponent<GameboardRenderer>().enabled;

        //turn off the path rendering on any agent
        _agent.GetComponent<GameboardAgentPathRenderer>().enabled =
            !_agent.GetComponent<GameboardAgentPathRenderer>().enabled;
    }

    private void HandleTouch()
    {
        //in the editor we want to use mouse clicks on phones we want touches.
        #if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        #else
        //if there is a touch call our function
        if (Input.touchCount <= 0)
            return;

        var touch = Input.GetTouch(0);

        //if there is no touch or touch selects UI element
        if (Input.touchCount <= 0 )
            return;
        if (touch.phase == UnityEngine.TouchPhase.Began)
        #endif
        {
            #if UNITY_EDITOR
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            #else
            Ray ray = _camera.ScreenPointToRay(touch.position);
            #endif
            //project the touch point from screen space into 3d and pass that to your agent as a destination
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
                }
                else
                {
                    _agent.SetDestination(hit.point);
                }
            }
        }
    }
}
