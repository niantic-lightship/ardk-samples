using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ExportScanPanel : MonoBehaviour
{
    [Tooltip("The slider used to show the export progress")] [SerializeField]
    private Text _exportText;

    [Tooltip("Used to animate progress")] [SerializeField]
    private Text _progressText;

    private bool _isWorking;

    void Update()
    {
        if (_isWorking)
        {
            int numberOfDots = Mathf.RoundToInt(Time.time) % 3 + 1;
            string dotsText = string.Concat(Enumerable.Range(0, numberOfDots).Select(t => "."));
            _progressText.text = "Exporting" + dotsText;
        }
        else
        {
            _progressText.text = "Export Complete";
        }
    }

    public void SetExportStatusText(bool isWorking, string path)
    {
        this._isWorking = isWorking;
        _exportText.text = path;
    }
}
