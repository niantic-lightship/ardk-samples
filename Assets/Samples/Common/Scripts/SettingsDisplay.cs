using System.Collections;
using System.Collections.Generic;
using Niantic.Lightship.AR;
using Niantic.Lightship.AR.Loader;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Management;

public class SettingsDisplay : MonoBehaviour
{
    [Header("Depth")]
    [SerializeField] private Toggle m_DepthEnabledToggle;

    [SerializeField] private Text m_DepthFramerateText;

    [Header("VPS")]
    [SerializeField] private Toggle m_VpsEnabledToggle;

    [Header("Playback")]
    [SerializeField] private Toggle m_PlaybackOnEditorEnabledToggle;
    [SerializeField] private Toggle m_PlaybackOnDeviceEnabledToggle;

    [Header("Debug")]
    [SerializeField] private Toggle m_ImageProcessingModeToggle;

    void Start()
    {
        var settings = LightshipSettings.Instance;

        m_DepthEnabledToggle.isOn = settings.UseLightshipDepth;
        m_DepthFramerateText.text = $"Framerate: {settings.LightshipDepthFrameRate}";

        m_VpsEnabledToggle.isOn = settings.UseLightshipPersistentAnchor;

        m_PlaybackOnEditorEnabledToggle.isOn = settings.UsePlaybackOnEditor;
        m_PlaybackOnDeviceEnabledToggle.isOn = settings.UsePlaybackOnDevice;

        var imageProcessingMode =
            PlayerPrefs.GetInt("ImageProcessMode", 0) == 0 ? "CPU" : "GPU";

        m_ImageProcessingModeToggle.isOn = imageProcessingMode == "GPU";
        m_ImageProcessingModeToggle.onValueChanged.AddListener(OnImageProcessingToggleChanged);
    }

    // The toggle is disabled in the scene and the _PlatformAdapaterManager code that uses this PlayerPrefs value is
    // gone too, but am leaving this code to toggle GPU/CPU at runtime here, because it'll be useful again once we're
    // revisiting the GPU option.
    private void OnImageProcessingToggleChanged(bool isEnabled)
    {
        PlayerPrefs.SetInt("ImageProcessMode", isEnabled ? 1 : 0);
        PlayerPrefs.Save();

        var imageProcessingMode =
            PlayerPrefs.GetInt("ImageProcessMode", 0) == 0 ? "CPU" : "GPU";
        Debug.Log("Set image processing mode to: " + imageProcessingMode);

        ReinitializeLoader();
    }

    private void ReinitializeLoader()
    {
        var loader = XRGeneralSettings.Instance.Manager.activeLoader;
        loader.Deinitialize();
        loader.Initialize();
    }
}
