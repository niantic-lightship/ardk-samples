// Copyright 2023 Niantic, Inc. All Rights Reserved.
using UnityEngine;
using UnityEngine.UI;


public class VersionRead : MonoBehaviour
{

    [SerializeField]
    private Text uiTextBox;

    private const string SamplesVersion = "3.0.0";

    // Start is called before the first frame update
    void Start()
    {
        uiTextBox.text = "ARDK: " + Niantic.Lightship.AR.Settings.Metadata.Version +
            "\n" + "Shared AR: " + Niantic.Lightship.SharedAR.Settings.Metadata.SharedArVersion +
            "\n" + "Samples: " + SamplesVersion;
    }

}
