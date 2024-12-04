
using UnityEngine;
using UnityEngine.UI;
using System.IO;
public class OnDevicePersistence : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField]
    private Mapper _mapper;
    
    [SerializeField]
    private Tracker _tracker;
    
    [Header("UX - Status")]
    [SerializeField]
    private Text _statusText;

    [Header("UX - Create/Load")]
    [SerializeField] 
    private GameObject _createLoadPanel;
    
    [SerializeField]
    private Button _createMapButton;
    
    [SerializeField]
    private Button _loadMapButton;
    
    [Header("UX - Scan Map")]
    [SerializeField] 
    private GameObject _scanMapPanel;
    
    [SerializeField]
    private Button _startScanning;
    
    [SerializeField]
    private Button _exitScanMapButton;

    [Header("UX - Scanning Animation")] 
    [SerializeField]
    private GameObject _scanningAnimationPanel;
    
    [Header("UX - In Game")]
    [SerializeField] 
    private GameObject _inGamePanel;
    
    [SerializeField]
    private Button _placeCubeButton;
    
    [SerializeField]
    private Button _deleteCubesButton;
    
    [SerializeField]
    private Button _exitInGameButton;

    //files to save to
    public static string k_mapFileName = "ADHocMapFile";
    public static string k_objectsFileName = "ADHocObjectsFile";
    
    /// <summary>
    /// Set up to main menu on start
    /// </summary>
    void Start()
    {
        SetUp_CreateMenu();
    }
    
    /// <summary>
    /// Exit to main menu
    /// </summary>
    private void Exit()
    {
        _statusText.text = "";
        
        //make sure all menu are destroyed
        Teardown_InGameMenu();
        Teardown_LocalizeMenu();
        Teardown_ScanningMenu();
        Teardown_CreateMenu();
        
        //tracking if running needs to be stopped on exit.
        _tracker.StopAndDestroyAnchor();

        //go back to the main menu
        SetUp_CreateMenu();
        
    }
    
    /// <summary>
    /// Create Map / Load map functions
    /// </summary>
    private bool CheckForSavedMap(string MapFileName)
    {
        var path = Path.Combine(Application.persistentDataPath, MapFileName);
        if (System.IO.File.Exists(path))
        {
            return true;
        }
        return false;
    }
    
    private void SetUp_CreateMenu()
    {
        //hide other menus
        Teardown_InGameMenu();
        Teardown_ScanningMenu();
        Teardown_LocalizeMenu();
        
        _createLoadPanel.SetActive(true);
        
        _createMapButton.onClick.AddListener(SetUp_ScanMenu);
        _loadMapButton.onClick.AddListener(SetUp_LocalizeMenu);
        
        _createMapButton.interactable=true;

        //if there is a saved map enable the load button.
        if(CheckForSavedMap(k_mapFileName))
        {
            _loadMapButton.interactable=true;
        }
        else
        {
            _loadMapButton.interactable=false;
        }
    }
    
    private void Teardown_CreateMenu()
    {
        _createLoadPanel.gameObject.SetActive(false);   
        _createMapButton.onClick.RemoveAllListeners();
        _loadMapButton.onClick.RemoveAllListeners();
    }
    
    /// <summary>
    /// Scan Map functions
    /// </summary>
    private void SetUp_ScanMenu()
    {
        Teardown_CreateMenu();
        _scanMapPanel.SetActive(true);
        _startScanning.onClick.AddListener(StartScanning);
        _exitScanMapButton.onClick.AddListener(Exit);
        
        _startScanning.interactable=true;
        _exitScanMapButton.interactable=true;
    }
    
    private void Teardown_ScanningMenu()
    {
        _startScanning.onClick.RemoveAllListeners();
        _exitScanMapButton.onClick.RemoveAllListeners();
        _scanMapPanel.gameObject.SetActive(false);
        _mapper._onMappingComplete -= MappingComplete;
        _mapper.StopMapping();
    }
    
    private void StartScanning()
    {
        _startScanning.interactable = false;
        _statusText.text = "Look Around to create map";
        _mapper._onMappingComplete += MappingComplete;
        float time = 5.0f;
        _mapper.RunMappingFor(time);
        
        _scanningAnimationPanel.SetActive(true);
    }
    
    private void MappingComplete(bool success)
    {
        if (success)
        {
            //clear out any cubes
            DeleteCubes();
            _scanningAnimationPanel.SetActive(false);
            
            //jump to localizing.
            SetUp_LocalizeMenu();
        }
        else
        {
            //failed to make a map try again.
            _startScanning.interactable = true;
            _statusText.text = "Map Creation Failed Try Again";
        }
        _mapper._onMappingComplete -= MappingComplete;
    }

    /// <summary>
    /// Localization to Map functions
    /// </summary>
    private void SetUp_LocalizeMenu()
    {
        Teardown_CreateMenu();
        Teardown_ScanningMenu();
        //go to tracking and localise to the map.
        _statusText.text = "Move Phone around to localize to map";
        _tracker._tracking += Localized;
        _tracker.StartTracking();
        
        _scanningAnimationPanel.SetActive(true);
    }
    
    private void Teardown_LocalizeMenu()
    {
        _tracker._tracking -= Localized;
        _scanningAnimationPanel.SetActive(false);
    }
    
    private void Localized(bool localized)
    {
        //once we are localised we can open the main menu.
        if (localized == true)
        {
            _statusText.text = "";
            _tracker._tracking -= Localized;
            SetUp_InGameMenu();
            LoadCubes();
            _scanningAnimationPanel.SetActive(false);
        }
        else
        {
            //failed exit out.
            Exit();
        }
    }
    /// <summary>
    /// In game functions
    /// </summary>
    private void SetUp_InGameMenu()
    {
        Teardown_LocalizeMenu();
        Teardown_ScanningMenu();
        
        _inGamePanel.SetActive(true);
        _placeCubeButton.onClick.AddListener(PlaceCube);
        _deleteCubesButton.onClick.AddListener(DeleteCubes);
        _exitInGameButton.onClick.AddListener(Exit);
        
        _placeCubeButton.interactable=true;
        _exitInGameButton.interactable=true;
    }
    
    private void Teardown_InGameMenu()
    {
        _placeCubeButton.onClick.RemoveAllListeners();
        _exitInGameButton.onClick.RemoveAllListeners();
        _deleteCubesButton.onClick.RemoveAllListeners();
        _inGamePanel.gameObject.SetActive(false);
    }

    /// <summary>
    /// Manging the cude placement/storage and anchoring to map function
    /// </summary>
    private GameObject CreateAndPlaceCube(Vector3 localPos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        
        //add it under the anchor on our map.
        _tracker.AddObjectToAnchor(go);
        go.transform.localPosition = localPos;
        //make it smaller.
        go.transform.localScale = new Vector3(0.2f,0.2f,0.2f);
        return go;
    }

    private void PlaceCube()
    {
        //place a cube 2m in front of the camera.
        var pos = Camera.main.transform.position + (Camera.main.transform.forward*2.0f);
        var go = CreateAndPlaceCube(_tracker.GetAnchorRelativePosition(pos));
        var fileName = OnDevicePersistence.k_objectsFileName;
        var path = Path.Combine(Application.persistentDataPath, fileName);
      
        using (StreamWriter sw = File.AppendText(path))
        {
            sw.WriteLine(go.transform.localPosition);
        }
    }

    private void LoadCubes()
    {
        var fileName = OnDevicePersistence.k_objectsFileName;
        var path = Path.Combine(Application.persistentDataPath, fileName);
        if (File.Exists(path))
        {
            using (StreamReader sr = new StreamReader(path))
            {
                while (sr.Peek() >= 0)
                {
                    var pos = sr.ReadLine();
                    var split1 = pos.Split("(");
                    var split2 = split1[1].Split(")");
                    var parts = split2[0].Split(",");
                    Vector3 localPos = new Vector3(
                        System.Convert.ToSingle(parts[0]),
                        System.Convert.ToSingle(parts[1]),
                        System.Convert.ToSingle(parts[2])
                    );

                    CreateAndPlaceCube(localPos);
                }
            }
        }
    }
    
    private void DeleteCubes()
    {
        //delete from the file.
        var fileName = OnDevicePersistence.k_objectsFileName;
        var path = Path.Combine(Application.persistentDataPath, fileName);
        File.Delete(path);
        
        //delete from in game.
        if (_tracker.Anchor)
        {
            for (int i = 0; i < _tracker.Anchor.transform.childCount; i++)
                Destroy(_tracker.Anchor.transform.GetChild(i).gameObject);
        }
    }
}
