// Copyright 2022-2025 Niantic.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneSwitcher : MonoBehaviour, ISerializationCallbackReceiver
{
    [SerializeField]
    private Object _targetScene;

    [SerializeField] [HideInInspector]
    private string _targetSceneName;

    [SerializeField] private bool _stopSubsystems;

    private HashSet<string> PortraitScenes = new HashSet<string>() {"Home","RemoteAuthoring","CompassScene" };

    private void Start()
    {
        //in-case no button
        if (TryGetComponent<Button>(out Button button))
        {
            //check if it has a listener, if then hook it up.
            int listenerCount = button.onClick.GetPersistentEventCount();
            if (listenerCount == 0)
            {
                button.onClick.AddListener(SwitchToScene);
            }
        }
    }

    public void SwitchToScene()
    {
        OrientationPicker();
        SceneManager.LoadScene(_targetSceneName, LoadSceneMode.Single);
    }

    public void OnBeforeSerialize()
    {
#if UNITY_EDITOR
        if (_targetScene != null && _targetScene is SceneAsset sceneAsset)
        {
            _targetSceneName = sceneAsset.name;
        }
#endif
    }

    public void OnAfterDeserialize()
    {
    }
    private void OrientationPicker()
    {
        if (PortraitScenes.Contains(_targetSceneName))
        {
            Screen.orientation = ScreenOrientation.Portrait;
        }
        else
        {
            Screen.orientation = ScreenOrientation.AutoRotation;
        }
    }
}
