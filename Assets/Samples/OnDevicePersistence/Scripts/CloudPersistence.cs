
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CloudPersistence
/// This class manages the UI states.
/// It maps the screens and buttons to actions for scanning maps, localising, creating / deleting objects
/// </summary>
public class CloudPersistence : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField]
    private Mapper _mapper;
    
    [SerializeField]
    private Tracker _tracker;
    
    [SerializeField]
    private DatastoreManager _datastoreManager;
    
    [Header("UX - Status")]
    [SerializeField]
    private Text _statusText;

    [Header("UX - Room Name Input")]
    [SerializeField]
    private InputField _roomNameInputField;
    
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
    
    
    [Header("UX - Localization")] 
    [SerializeField]
    private GameObject _localizationPanel;
    
    [SerializeField]
    private Button _exitLocalizeButton;
    
    [Header("UX - In Game")]
    [SerializeField] 
    private GameObject _inGamePanel;
    
    [SerializeField]
    private Button _placeCubeButton;
    
    [SerializeField]
    private Button _deleteCubesButton;
    
    [SerializeField]
    private Button _exitInGameButton;
    
    private string _roomName;
    private bool _sendNewMap;
    private bool _waitForMap;
    private bool _clearOnLoad;

    /// <summary>
    /// Set up to main menu on start
    /// </summary>
    void Start()
    {
        //we want to use the datastore so turning off the local file save option
        _tracker._loadFromFile = false;
        _mapper._saveToFile = false;

        SetUp_CreateMenu();
    }

    private void Update()
    {
        if (!_datastoreManager._networkRunning)
            return;
        
        //if we have a map waiting to send we can send it now.
        if (_sendNewMap)
        {
            _sendNewMap = false;
            _datastoreManager.SaveMapToDatastore();
        }
        
        //if we are waiting to localise we can localise now
        if (_waitForMap)
        {
            _waitForMap = false;
            //go to tracking and localise to the map.
            _statusText.text = "Move Phone around to localize to map";
            _tracker._tracking += Localized;
            _tracker.StartTracking();
                    
            _scanningAnimationPanel.SetActive(true);
        }
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
        
        StartCoroutine(ClearTrackingAndMappingState());
        
        _datastoreManager.LeaveDataStore();
  
        _waitForMap = false;
        _sendNewMap = false;
        _clearOnLoad = false;
        
        //go back to the main menu
        SetUp_CreateMenu();
    }

    private IEnumerator ClearTrackingAndMappingState()
    {
        // Both ARPersistentAnchorManager and 
        // need to be diabled before calling ClearDeviceMap()

        _mapper.ClearAllState();
        yield return null;

        _tracker.ClearAllState();
        yield return null;
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

        _roomNameInputField.gameObject.SetActive(true);
        
        _loadMapButton.interactable=true;
    
    }
    
    private void Teardown_CreateMenu()
    {
        _roomNameInputField.gameObject.SetActive(false);

        _createLoadPanel.gameObject.SetActive(false);   
        _createMapButton.onClick.RemoveAllListeners();
        _loadMapButton.onClick.RemoveAllListeners();
    }

    bool ValidateRoomName(string roomName)
    {
        if ((roomName.Length == 0) || (!Regex.IsMatch(roomName, "^[a-zA-Z0-9]*$")))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Scan Map functions
    /// </summary>
    private void SetUp_ScanMenu()
    {
        if (!ValidateRoomName(_roomNameInputField.text))
        {
            _statusText.text = "Invalid name";
            return;
        }
     
        _roomName = _roomNameInputField.text;
        
        _statusText.text = "";
        
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
            _scanningAnimationPanel.SetActive(false);
            
            //Create the data store room
            _datastoreManager.CreateOrJoinDataStore(_roomName);
            _sendNewMap = true;
            _clearOnLoad = true;
            _waitForMap = true;
            
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
       
        if (!ValidateRoomName(_roomNameInputField.text))
        {
            _statusText.text = "Invalid name";
            return;
        }
     
        _roomName = _roomNameInputField.text;
        
        Teardown_CreateMenu();
        Teardown_ScanningMenu();
        
        _localizationPanel.SetActive(true);
        _exitLocalizeButton.onClick.AddListener(Exit);
        
        _datastoreManager.CreateOrJoinDataStore(_roomName);
        _waitForMap = true;
    
        _statusText.text = "Waiting for device map....";
        

    }
    
    private void Teardown_LocalizeMenu()
    {
        _tracker._tracking -= Localized;
        _scanningAnimationPanel.SetActive(false);
        
        _localizationPanel.SetActive(false);
        _exitLocalizeButton.onClick.RemoveAllListeners();
    }
    
    private void Localized(bool localized)
    {
        //once we are localised we can open the main menu.
        if (localized == true)
        {
            _statusText.text = "";
            _tracker._tracking -= Localized;
            SetUp_InGameMenu();
            _scanningAnimationPanel.SetActive(false);

            if (_clearOnLoad)
            {
                _clearOnLoad = false;
                _datastoreManager.DeleteCubes();
            }
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
        _placeCubeButton.onClick.AddListener(_datastoreManager.PlaceCube);
        _deleteCubesButton.onClick.AddListener(_datastoreManager.DeleteCubes);
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
}
