// Copyright 2022-2025 Niantic.
using UnityEngine;
using UnityEngine.UI;


public class VersionRead : MonoBehaviour
{

    [SerializeField]
    private Text uiTextBox;

    private const string SamplesVersion = "3.13.0";

    void Awake(){
        Screen.orientation = ScreenOrientation.Portrait;
    }

    // Start is called before the first frame update
    void Start()
    {
        
        uiTextBox.text = "ARDK: " + Niantic.Lightship.AR.Settings.Metadata.Version +
            "\n" + "Shared AR: " + Niantic.Lightship.SharedAR.Settings.Metadata.SharedArVersion +
            "\n" + "Samples: " + SamplesVersion;
    }

}
