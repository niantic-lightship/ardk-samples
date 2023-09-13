using UnityEngine;
using UnityEngine.UI;


public class VersionRead : MonoBehaviour
{

    [SerializeField]
    private Text uiTextBox;

    // Start is called before the first frame update
    void Start()
    {
        uiTextBox.text = "ARDK: " + Niantic.Lightship.AR.Utilities.ARDKVersion.Version;
    }

}
