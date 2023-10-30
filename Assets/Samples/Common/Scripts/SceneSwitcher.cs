// Copyright 2023 Niantic, Inc. All Rights Reserved.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    private HashSet<string> PortraitScenes = new HashSet<string>() {"Home","PersistentAR" };

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
