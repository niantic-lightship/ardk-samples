// Copyright 2022-2025 Niantic.
using UnityEngine;
using UnityEngine.UI;


public class VersionRead : MonoBehaviour
{

    public Text uiTextBox;

    private const string SamplesVersion = "3.16.0";

    void Awake(){
        Screen.orientation = ScreenOrientation.Portrait;
    }

    // Start is called before the first frame update
    void Start()
    {
        
        uiTextBox.text = "SDK: " + Niantic.Lightship.AR.Settings.Metadata.Version +
            "\n" + "Shared: " + Niantic.Lightship.SharedAR.Settings.Metadata.SharedArVersion +
            "\n" + "Samples: " + SamplesVersion;
    }

}
