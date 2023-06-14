using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;

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

    public void SwitchToScene()
    {
        if (_stopSubsystems)
        {
            StartCoroutine(SceneSwitchAfterStoppingSubsystems());
        }
        else
        {
            SceneManager.LoadScene(_targetSceneName, LoadSceneMode.Single);
        }

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

    IEnumerator SceneSwitchAfterStoppingSubsystems()
    {
        if (XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager != null)
        {
            var loader = XRGeneralSettings.Instance.Manager.activeLoader;
            if (loader != null)
            {
                var occlusionSubsystem = loader.GetLoadedSubsystem<XROcclusionSubsystem>();
                if (occlusionSubsystem != null)
                {
                    occlusionSubsystem.Stop();
                }
            }
        }

        yield return null;

        SceneManager.LoadScene(_targetSceneName, LoadSceneMode.Single);

    }
}
