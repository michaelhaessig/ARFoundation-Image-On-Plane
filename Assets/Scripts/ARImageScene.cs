using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Debug = UnityEngine.Debug;
using Lean.Touch;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

/*
 * @author Michael Hässig
 * @email michael.haessig (at) gmail (dot) com
 */
public abstract class ARImageScene : ARScene {

    public static float PIXEL_PER_UNIT = 1000; // change this to modify initial sprite size

    public GameObject imageCanvasPrefab;
    public GameObject imageRemoveIconPrefab;

    public Sprite demoSprite;

    public LayerMask imageLayerMask;

    public LeanSelect leanSelect;

    protected Light topLight;

    protected Sprite imageSprite;

    protected GameObject imageInRoom;
    protected GameObject imageRemoveIcon;

    public bool DataReady = false;

    public static PixabayImage CurrentImage;

    // Use this for initialization
    protected override void Start() {

   
        base.Start();

        // setup needed data in a coroutine
        StartCoroutine(SetupData());
    }

    protected override void Update()
    {
        base.Update();
    }

    protected virtual void OnEnable()
    {
        // camera events for light updates
        ARSubsystemManager.cameraFrameReceived += OnCameraFrameReceived;
        ARScene.ARReadyStateChanged += OnARReadyStateChanged;

        // Hook events
        LeanTouch.OnFingerTap += FingerTap;
        LeanTouch.OnFingerSet += FingerSet;
    }


    protected virtual void OnDisable()
    {
        // camera events for light updates
        ARSubsystemManager.cameraFrameReceived -= OnCameraFrameReceived;
        ARScene.ARReadyStateChanged -= OnARReadyStateChanged;

        // Unhook events
        LeanTouch.OnFingerTap -= FingerTap;
        LeanTouch.OnFingerSet -= FingerSet;
    }

    protected virtual void OnARReadyStateChanged(ARReadyStateEventArgs obj)
    {
        // allow override
    }

    protected virtual void OnCameraFrameReceived(ARCameraFrameEventArgs obj)
    {
        if (topLight != null)
        {
            // own method call ( supports virtual )
            UpdateLight(obj.lightEstimation);
        }

    }

    protected virtual void UpdateLight(LightEstimationData lightEstimation)
    {
        if (topLight != null)
        {
            if (lightEstimation.averageBrightness.HasValue)
            {
                // brightness multiplied for more visible effects
                var brightness = lightEstimation.averageBrightness.Value * 2f;

                // set brightness on directional light
                topLight.intensity = brightness;               
            }

            if(lightEstimation.colorCorrection.HasValue)
            {
                // get color correction value ( we use it as base )
                var colorCorrection = lightEstimation.colorCorrection.Value;

                // ligh tcolor change based on colorCorrection -> does not give good results -> we keep the light white
                //topLight.color = new Color(colorCorrection.r, colorCorrection.g, colorCorrection.b) * topLight.intensity;

                // get sprite renderer
                SpriteRenderer spriteRenderer = imageInRoom.GetComponent<SpriteRenderer>();

                // WE SET THE SPRITE SHADER COLOR instead of the light color ( allows for more dramatic changes when lights are really low brightness )
                // calculate sprite color overlay based on colorColoraction and light intensity
                var spriteColor = new Color(colorCorrection.r, colorCorrection.g, colorCorrection.b) * topLight.intensity;
                spriteRenderer.material.color = new Color(spriteColor.r, spriteColor.g, spriteColor.b, 1.0f);
            }

            if (lightEstimation.averageColorTemperature.HasValue)
            {
                // set color colorTemperature on image TODO figure out if this can also be used on the sprite renderer color 
                topLight.colorTemperature = lightEstimation.averageColorTemperature.Value;
            }
        }

    }


    protected virtual void FingerSet(LeanFinger finger)
    {
        // Ignore UI clicks
        if (finger.StartedOverGui == true || finger.IsOverGui == true)
        {
            return;
        }

        /*
         * IMPORTANT to execute this in FingerSet instead of FingerTap to allow translate/scale without first selecting it       
         * 
         * select gameObjects with the LeanSelectable script ( uses raycasting ) 
        */
        leanSelect.SelectScreenPosition(finger);
    }

    protected virtual void FingerTap(LeanFinger finger)
    {
        // Ignore UI clicks
        if (finger.StartedOverGui == true || finger.IsOverGui == true)
        {
            return;
        }

        // Ignore Tap if ARScene is not ready yet
        if (!ARReady)
        {
            return;
        }

        // Image Data not downloaded yet
        // IMPORTANT: we already show tap animation without checking if DataReady is set , because those are 2 different states that are not changed in sync, we just assume image Data is downloaded before ARReady is true
        if (!DataReady)
        {
            return;
        }


        Stopwatch timeWatch = new Stopwatch();
        timeWatch.Start();

        // call AR Ready version of event
        ARReadyFingerTap(finger);

        timeWatch.Stop();
        Debug.Log(string.Format("----> image tap position took {0} ms to complete", timeWatch.ElapsedMilliseconds));

    }

    // only gets called when ARReady is true ( overwrite for custom logic )
    protected virtual void ARReadyFingerTap(LeanFinger finger)
    {

    }

    protected virtual void InstantiateGameObjects()
    {
        Stopwatch initWatch = new Stopwatch();
        initWatch.Start();

        imageInRoom = InstantiateImageWithSprite(imageSprite);

        imageRemoveIcon = InstantiateRemoveIcon(imageInRoom);

        initWatch.Stop();
        Debug.Log(string.Format("----> InstantiateGameObjects took {0} ms to complete", initWatch.ElapsedMilliseconds));
    }


    protected virtual GameObject InstantiateImageWithSprite(Sprite sprite)
    {
        // instantiate as child of this gameObject
        GameObject imageObject = Instantiate<GameObject>(imageCanvasPrefab, gameObject.transform);

        // hide object
        imageObject.SetActive(false);

        // set sprite on renderer
        SpriteRenderer spriteRenderer = imageObject.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;

        // add box collider ( auto calculates dimensions )
        imageObject.AddComponent<BoxCollider>();

        // add directional light
        AddTopLightAsChild(imageObject);


        // add event listeners
        var leanSelectable = imageObject.GetComponent<LeanSelectable>();


        if (leanSelectable != null)
        {
            // intialize events ( only needed when LeanSelectable is added via code - currently not the case )
            if (leanSelectable.OnSelectUp == null)
            {
                leanSelectable.OnSelectUp = new LeanSelectable.LeanFingerEvent();
                leanSelectable.OnDeselect = new UnityEngine.Events.UnityEvent();
            }

            // image OnSelectUp event instead of OnSelect to include Tab information  
            leanSelectable.OnSelectUp.AddListener(ImageSelectUp);
            leanSelectable.OnDeselect.AddListener(ImageDeselect);
        }
        else
        {
            Debug.LogError("missing Lean Selectalbe script on prefab");
        }

        return imageObject;
    }

    protected virtual GameObject InstantiateRemoveIcon(GameObject followImage)
    {
        // instantiate as child of this gameObject
        GameObject removeIconObject = Instantiate<GameObject>(imageRemoveIconPrefab, gameObject.transform);

        // hide object
        removeIconObject.SetActive(false);

        // get ARImageRemoveIcon component to set follow image
        var removeIcon = removeIconObject.GetComponent<ARImageRemoveIcon>();
        removeIcon.followImage = followImage;

        var leanSelectable = removeIconObject.GetComponent<LeanSelectable>();

        if (leanSelectable != null)
        {
            // when remove icon gets selected remove image
            leanSelectable.OnSelectUp.AddListener(RemoveIconSelectUp);
        }
        else
        {
            Debug.LogError("missing Lean Selectalbe script on remove prefab");
        }

        return removeIconObject;
    }

    protected virtual void AddTopLightAsChild(GameObject imageObject)
    {
        // Make a game object
        GameObject lightGameObject = new GameObject("TopLight");

        // Add the light component
        topLight = lightGameObject.AddComponent<Light>();

        // set type to directional
        topLight.type = LightType.Directional;

        // Set color and position
        topLight.color = Color.white;

        // position 1meter above and 1 meter back from the image
        lightGameObject.transform.position = new Vector3(0, 1, -1);
        // set rotation to point downwards
        lightGameObject.transform.rotation = Quaternion.Euler(60, 0, 0);

        // set light as child of image
        lightGameObject.transform.parent = imageObject.transform;
    }

 


    // private because we add event listener direct as delegate ( not sure if virtual polymorphism works with delegates )
    private void ImageSelectUp(LeanFinger finger)
    {
        Debug.Log("---> ImageSelectUp tap count: " + finger.TapCount + " tap: " + finger.Tap);

        // only if the selection was triggered by a tap we keep the selection ( to show/hide the remove icon )
        if (finger.Tap)
        {
            // show remove icon
            imageRemoveIcon.SetActive(true);
        } 
        // IMPORTANT:  selection by transform/scale - manual Deselect to allow Re Selection via Tap 
        else 
        {
            var leanSelectableImage = imageInRoom.GetComponent<LeanSelectable>();

            leanSelectableImage.Deselect();
        } 
    }

    private void ImageDeselect()
    {
        Debug.Log("---> ImageDeselect");

        if(imageRemoveIcon != null)
        {
            // hide remove icon
            imageRemoveIcon.SetActive(false);
        }
    }

    private void RemoveIconSelectUp(LeanFinger finger)
    {
        Debug.Log("---> RemoveImage Up count: " + finger.TapCount + " tap: " + finger.Tap);

        // only use tap to make it more explizit 
        if(finger.Tap)
        {
            // IMPORTANT hack coroutine to execute code on the next frame ( this avoids having to set a flag and check the flag on update .. not sure if this is good practice )
            // we cannot destory/deactivate any LeanSelectable gameObjects because the LeanSelectable.FingerUp script loop throws exception because the LeanSelectable.Instances delete itself when destoried/deactivated ) 
            StartCoroutine(RemoveIconOnNextFrame());
        }
    }

    private IEnumerator RemoveIconOnNextFrame()
    {
        // skips this frame 
        yield return null;

        // next frame
        OnRemoveIconClicked();
    }

    protected virtual void OnRemoveIconClicked()
    {
        // set image inactive
        imageInRoom.SetActive(false);
    }

    protected virtual IEnumerator SetupData()
    {
        DataReady = false;

        // try download if image is set - if not try to use Sprite set via Editor
        if(CurrentImage != null)
        {
            // download image
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(CurrentImage.largeImageURL);
            yield return www.SendWebRequest();

            var texture = DownloadHandlerTexture.GetContent(www);

            //set image
            imageSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), PIXEL_PER_UNIT);

        }
        else if(demoSprite != null)
        {
            imageSprite = demoSprite;
        }


        if(imageSprite == null)
        {
            Debug.LogError("missing sprite, for dev setup demoSprite in editor");
        }

        // create all neede gameobjects with active = false 
        InstantiateGameObjects();

        // set ready flag
        DataReady = true;
    }


    public void OpenSelectionScence()
    {
        SceneManager.LoadScene("ImageSelectionScene");
    }

}
