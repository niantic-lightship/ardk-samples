using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Dynamically generates a complete UI structure, including a Canvas, ScrollView,
/// pinned title/version text, and a grid of buttons moved from a source hierarchy.
/// Sections are foldable by clicking their headers.
/// </summary>
public class DynamicUIGenerator : MonoBehaviour
{
    [Header("Source References")]
    [Tooltip("The root object in the hierarchy containing the groups of buttons to be moved.")]
    public Transform rootObject;

    [Header("Prefab References")]
    [Tooltip("The prefab for the header text. This is still instantiated.")]
    public Text headerPrefab;
    [Tooltip("The font to be used for dynamically created text elements (Title, Version).")]
    public Font uiFont;

    [Header("UI Settings")]
    [Tooltip("The title to display at the top of the UI.")]
    public Sprite titleImage;
    public float widthPercentage = 0.4f;
    
    [Tooltip("The version text to display at the bottom.")]
    public string version = "v1.0.0";
    public Color versionTextColor = Color.black;
    public int versionFontSize = 96;
    
    [Tooltip("The number of columns for the button grid.")]
    public int landscapeColumnCount = 6;
    public int portraitColumnCount = 3;
        
    [Tooltip("The spacing between elements in the grid.")]
    public Vector2 gridSpacing = new Vector2(10, 10);

    [Header("Dynamic References (Auto-assigned)")]
    [Tooltip("The dynamically created Text element for the title.")]
    public Text titleText;
    [Tooltip("The dynamically created Text element for the version text.")]
    public Text versionText;
    [Tooltip("The dynamically created parent for the generated UI elements.")]
    public RectTransform contentParent;
    
    [Header("Button config")]
    public Sprite uiSprite;
    public int headerFontSize = 46;
    public Color headerColor = Color.black;
    public Color headerTextColor = Color.white;
    public float headerPixesPerUnit = 0.1f;
    
    public Color buttonColor = Color.white;
    public Color buttonTextColor = Color.black;
    public float buttonPixesPerUnit = 0.1f;

    [Header("Canvas config")]
    public Color canvasColor = Color.white;
    
    // Private fields for orientation handling
    private List<GridLayoutGroup> createdGrids = new List<GridLayoutGroup>();
    private bool isCurrentlyLandscape =false;
    private CanvasScaler canvasScaler;
    private int lastScreenWidth = 0;
    private int lastScreenHeight = 0;
    private RectTransform scrollRectTransform;
    private RectTransform titleRect;
    public float buttonHeight = 150f;
    
    void Start()
    {
        // Ensure required prefabs and sources are assigned
        if (rootObject == null || headerPrefab == null || uiFont == null)
        {
            Debug.LogError("Please assign 'rootObject', 'headerPrefab', and 'uiFont' in the Inspector.", this);
            this.enabled = false;
            return;
        }

        // Create the entire UI structure from scratch
        CreateFullUIStructure();

        // --- SAFEGUARD ---
        if (contentParent.IsChildOf(rootObject))
        {
            Debug.LogError("DynamicUIGenerator Error: The 'contentParent' cannot be a child of the 'rootObject'. This configuration creates an infinite loop.", this.gameObject);
            this.enabled = false;
            return;
        }

        // Set the title and version text content
        //titleText.text = title;
        versionText.text = version;
        
        // Generate the UI by moving the buttons into the new structure
        GenerateUI();
    }

    void OnDisable()
    {
        //store the state of the menu so that that things stay open or closed.
    }

    /// <summary>
    /// Creates the entire Canvas and UI element hierarchy programmatically.
    /// </summary>
    private void CreateFullUIStructure()
    {
        // --- Event System ---
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        // --- Canvas ---
        GameObject canvasGo = new GameObject("DynamicUICanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        contentParent = canvas.transform as RectTransform;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler canvasScaler = canvasGo.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920,1080);
        
        canvasGo.AddComponent<GraphicRaycaster>();
        var canvasImg = canvasGo.AddComponent<Image>();
        canvasImg.color = canvasColor;
        
        // --- Scroll View ---
        GameObject scrollViewGo = new GameObject("ScrollView");
        scrollViewGo.transform.SetParent(canvas.transform, false);
        ScrollRect scrollRect = scrollViewGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollViewGo.AddComponent<Image>().color = new Color(0, 0, 0, 0.1f);
        scrollRectTransform = scrollViewGo.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0, 0);
        scrollRectTransform.anchorMax = new Vector2(1, 1);
        scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
        
        Vector2 sizeDelta = new Vector2(0, Screen.height * 0.2f);
        scrollRectTransform.offsetMin = new Vector2(0, 0);
        scrollRectTransform.offsetMax = new Vector2(-10, -sizeDelta.y);
        
        // --- Viewport ---
        GameObject viewportGo = new GameObject("Viewport");
        viewportGo.transform.SetParent(scrollRect.transform, false);
        viewportGo.AddComponent<Image>().color = new Color(1, 1, 1, 0.5f);
        viewportGo.AddComponent<Mask>().showMaskGraphic = false;
        RectTransform viewportRect = viewportGo.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.pivot = new Vector2(0, 1);
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        // --- Content ---
        GameObject contentGo = new GameObject("Content");
        contentGo.transform.SetParent(viewportGo.transform, false);
        contentParent = contentGo.AddComponent<RectTransform>();
        ContentSizeFitter sizeFitter = contentGo.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentParent.anchorMin = new Vector2(0, 1);
        contentParent.anchorMax = new Vector2(1, 1);
        contentParent.pivot = new Vector2(0.5f, 1);
        contentParent.sizeDelta = new Vector2(0, 0);

        // Link ScrollRect components
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentParent;
        
        //title image
        int offset = 100;
        GameObject titleGo = new GameObject("TitleImage");
        var img = titleGo.AddComponent<Image>();
        img.sprite= titleImage;
        titleGo.transform.SetParent(canvas.transform, false);
        img.color = Color.black;
        
        titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.sizeDelta = titleImage.bounds.size;
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(0, 1);
        titleRect.pivot = new Vector2(0.0f, 1);
        titleRect.anchoredPosition = new Vector2(0.0f, -offset);//0.0f);
        
        Canvas.ForceUpdateCanvases();
        
        var canvasRectTransform = canvasGo.GetComponent<RectTransform>();
        
        float canvasWidth = canvasRectTransform.rect.width;
        float newWidth = canvasWidth * widthPercentage;
        
        var lockedAspectRatio = titleRect.sizeDelta.x / titleRect.sizeDelta.y;
        // Calculate the new height to maintain the locked aspect ratio.
        float newHeight = newWidth / lockedAspectRatio;
        
        // Update both the width and height of the sizeDelta.
        titleRect.sizeDelta = new Vector2(newWidth, newHeight);
        
        // --- Version Text ---
        GameObject versionGo = new GameObject("VersionText");
        versionGo.transform.SetParent(canvas.transform, false);
        versionText = versionGo.AddComponent<Text>();
        versionText.font = uiFont;
        versionText.fontSize = versionFontSize;
        versionText.horizontalOverflow = HorizontalWrapMode.Overflow;
        versionText.verticalOverflow = VerticalWrapMode.Overflow;
        versionText.alignment = TextAnchor.UpperRight;
        versionText.color = versionTextColor;
        versionText.fontSize = versionFontSize;
        
        RectTransform versionRect = versionGo.GetComponent<RectTransform>();
        versionRect.anchorMin = new Vector2(0, 1);
        versionRect.anchorMax = new Vector2(1, 1);
        versionRect.pivot = new Vector2(0.5f, 0);
        
        versionRect.sizeDelta = new Vector2(0, 0);
        
        //get image size and use that to place the bottom of this text.
        //versionRect.anchoredPosition = new Vector2(0, -newHeight);
        versionRect.anchoredPosition = new Vector2(0, -offset);//0);

        var vr = versionGo.AddComponent<VersionRead>();
        vr.uiTextBox = versionText;
    }
    
    /// <summary>
    /// Builds the UI by creating headers and grids, then moving existing buttons into them.
    /// </summary>
    private void GenerateUI()
    {
        // Add a VerticalLayoutGroup to the content parent to stack headers and grids
        VerticalLayoutGroup verticalLayout = contentParent.gameObject.AddComponent<VerticalLayoutGroup>();
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = true;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childForceExpandHeight = false;
        verticalLayout.padding = new RectOffset(10, 10, 10, 10);
        verticalLayout.spacing = 15;

        List<Transform> groupsToProcess = new List<Transform>();
        foreach (Transform child in rootObject)
        {
            groupsToProcess.Add(child);
        }

        bool isFirstSection = true;
        
        // Iterate through the collected groups
        foreach (Transform groupTransform in groupsToProcess)
        {
            if (groupTransform.gameObject.activeSelf)
            {
                // 1. Create the Header Button
                GameObject headerButtonGo = new GameObject(groupTransform.name + " Header", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                headerButtonGo.transform.SetParent(contentParent, false);
                Button headerButton = headerButtonGo.GetComponent<Button>();
                
                // Add a nearly-transparent image to act as a reliable raycast target for the button.
                Image headerImage = headerButtonGo.GetComponent<Image>();
                headerImage.color = headerColor;
                headerImage.sprite = uiSprite;
                headerImage.type = Image.Type.Sliced;
                headerImage.pixelsPerUnitMultiplier = headerPixesPerUnit;

                // Give the button a fixed height so its clickable area is not zero.
                LayoutElement headerButtonLayout = headerButtonGo.GetComponent<LayoutElement>();
                headerButtonLayout.minHeight = 200f; // A fixed, reliable height. Adjust as needed.
                
                // Add the text to the button
                Text headerText = Instantiate(headerPrefab, headerButtonGo.transform);
                headerText.text = groupTransform.name;
                headerText.raycastTarget = false; // Make the text non-interactive so clicks pass through to the button.
                headerText.color = headerTextColor;
                
                RectTransform headerTextRect = headerText.GetComponent<RectTransform>();
                headerTextRect.anchorMin = Vector2.zero;
                headerTextRect.anchorMax = Vector2.one;
                headerTextRect.sizeDelta = Vector2.zero;

                // Configure header text to auto-scale
                headerText.resizeTextForBestFit = true;
                headerText.resizeTextMinSize = 10;
                headerText.resizeTextMaxSize = 96;
                headerText.horizontalOverflow = HorizontalWrapMode.Wrap;
                headerText.verticalOverflow = VerticalWrapMode.Overflow;
                headerText.alignment = TextAnchor.MiddleCenter;

                // 2. Create a container for the grid
                GameObject gridContainer = new GameObject(groupTransform.name + " Grid");
                RectTransform gridRect = gridContainer.AddComponent<RectTransform>();
                gridRect.SetParent(contentParent, false);

                // 3. Add and configure the GridLayoutGroup
                GridLayoutGroup gridLayout = gridContainer.AddComponent<GridLayoutGroup>();
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = portraitColumnCount;
                gridLayout.spacing = gridSpacing;
                gridLayout.padding = new RectOffset(10, 10, 10, 10);
                gridLayout.childAlignment = TextAnchor.UpperLeft;

                createdGrids.Add(gridLayout); // Store for later updates

                //force the update so thet width is correct!
                Canvas.ForceUpdateCanvases();
                float parentWidth = contentParent.rect.width;
                float totalHorizontalPadding = gridLayout.padding.left + gridLayout.padding.right;
                float totalSpacing = (portraitColumnCount - 1) * gridLayout.spacing.x;
                float cellWidth = (parentWidth - totalHorizontalPadding - totalSpacing) / portraitColumnCount;
                
                //button height half width.
                float maxHeight = Mathf.Min(cellWidth* 0.5f, 400.0f);
                buttonHeight = maxHeight;
                gridLayout.cellSize = new Vector2(cellWidth, maxHeight);

                // 4. Set up the folding logic
                headerButton.onClick.AddListener(() => {
                    gridContainer.SetActive(!gridContainer.activeSelf);
                });

                // 5. Move the existing buttons and configure their text
                List<Transform> buttonsToMove = new List<Transform>();
                foreach (Transform button in groupTransform)
                {
                    buttonsToMove.Add(button);
                }

                foreach (Transform buttonTransform in buttonsToMove)
                {
                    if (buttonTransform.gameObject.activeSelf)
                    {
                        buttonTransform.SetParent(gridLayout.transform, false);
                        Text buttonText = buttonTransform.GetComponentInChildren<Text>();
                        if (buttonText != null)
                        {
                            buttonText.resizeTextForBestFit = true;
                            buttonText.resizeTextMinSize = 8;
                            buttonText.resizeTextMaxSize = 96;
                            buttonText.horizontalOverflow = HorizontalWrapMode.Wrap;
                            buttonText.verticalOverflow = VerticalWrapMode.Truncate;
                            buttonText.color = buttonTextColor;
                        }

                        var buttonImage = buttonTransform.gameObject.GetComponent<Image>();
                        if (buttonImage != null)
                        {
                            buttonImage.color = buttonColor;
                            buttonImage.type = Image.Type.Sliced;
                            buttonImage.pixelsPerUnitMultiplier = buttonPixesPerUnit;
                        }
                    }
                }
                
                // 6. Set default visibility
                if (!isFirstSection)
                {
                    //gridContainer.SetActive(false);
                }
                isFirstSection = false;
            }
        }
    }
    void Update()
    {
        // Continuously check for orientation changes and apply layout updates if needed.
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            CheckAndApplyOrientation(false);
        }
    }

    /// <summary>
    /// Checks the current screen orientation and re-applies the layout if it has changed.
    /// </summary>
    private void CheckAndApplyOrientation(bool isInitialSetup)
    {
        bool isNowLandscape = Screen.width > Screen.height;
        if (isNowLandscape != isCurrentlyLandscape || isInitialSetup)
        {
            isCurrentlyLandscape = isNowLandscape;
            
            // Adjust Canvas Scaler to match the orientation for better scaling
            if (canvasScaler != null)
            {
                canvasScaler.matchWidthOrHeight = isCurrentlyLandscape ? 1f : 0f; // Match height in landscape, width in portrait
            }
            
            Canvas.ForceUpdateCanvases();
            
            Vector2 sizeDelta = new Vector2(0, Screen.height * 0.2f);
            
            scrollRectTransform.offsetMin = new Vector2(0, 0);
            scrollRectTransform.offsetMax = new Vector2(-10, -sizeDelta.y);
            
            float canvasWidth = contentParent.rect.width;
            float newWidth = canvasWidth * widthPercentage * (isCurrentlyLandscape?0.5f:1.0f);
        
            var lockedAspectRatio = titleRect.sizeDelta.x / titleRect.sizeDelta.y;
            // Calculate the new height to maintain the locked aspect ratio.
            float newHeight = newWidth / lockedAspectRatio;
        
            // Update both the width and height of the sizeDelta.
            titleRect.sizeDelta = new Vector2(newWidth, newHeight);

            UpdateAllGridLayouts();
        }
    }
    
    /// <summary>
    /// Updates all created grid layouts to match the current screen orientation.
    /// </summary>
    private void UpdateAllGridLayouts()
    {
        // Force the canvas to update its layout before we calculate cell sizes
    

        foreach (var gridLayout in createdGrids)
        {

            int columnCount = isCurrentlyLandscape ? landscapeColumnCount : portraitColumnCount;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = columnCount;

            float parentWidth = contentParent.rect.width;
            float totalHorizontalPadding = gridLayout.padding.left + gridLayout.padding.right;
            float totalSpacing = (columnCount - 1) * gridLayout.spacing.x;
            float cellWidth = (parentWidth - totalHorizontalPadding - totalSpacing) / columnCount;
            
            float maxHeight = Mathf.Min(cellWidth* 0.5f, 400.0f);
            buttonHeight = maxHeight;
            
            gridLayout.cellSize = new Vector2(cellWidth, buttonHeight);
        }
    }

}
