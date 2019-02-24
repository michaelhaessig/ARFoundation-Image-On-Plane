using UnityEngine;
using System.Collections;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

/*
 * @author Michael Hässig
 * @email michael.haessig (at) gmail (dot) com
 * 
 * Base AR Scene to setup tracking and show animations
 */
public abstract class ARScene : MonoBehaviour
{
    protected string ANIM_FADEON = "FadeOn";
    protected const string ANIM_FADEOFF = "FadeOff";

    public Animator MovePhoneAnim;
    public Animator TapToPlaceAnim;

    public static bool? _Debug; // static flag to set from other scene
    public bool DebugMode = false; // instance flag to set from editor
    public Text DebugText;
    private float deltaTime;

    // AR References
    public ARSessionOrigin ARSessionOrigin;
    public ARPointCloudManager ARPointCloudManager;
    public ARPlaneManager ARPlaneManager;

    // AR Prefabs
    public GameObject ARPlanePrefab;
    public GameObject ARPlanePrefabDebug;
    public GameObject ARPointCloudPrefab;
    public GameObject ARPointCloudPrefabDebug;

    // Plane Tracking for Ready Events
    public bool WaitForPlanes = false;
    public UnityEngine.XR.ARExtensions.PlaneDetectionFlags PlaneTypes = UnityEngine.XR.ARExtensions.PlaneDetectionFlags.Vertical | UnityEngine.XR.ARExtensions.PlaneDetectionFlags.Horizontal; // default allow vertical/horizontal planes ( bit operator | ) 
    protected bool hasPlanes = false;

    // Flags
    protected bool isTracking = false;
    public bool ARReady = false;

    public static event Action<ARReadyStateEventArgs> ARReadyStateChanged;

    public struct ARReadyStateEventArgs
    {
        public bool Ready { get; set; }
    }

    // Use this for initialization
    protected virtual void Start()
    {

        // map static debug flag
        if (_Debug.HasValue)
        {
            DebugMode = _Debug.Value;
        }

        // find sessionOrigin in scene if not referenced
        if (ARSessionOrigin == null)
        {
            ARSessionOrigin = FindObjectOfType<ARSessionOrigin>();
        }

        // find planeManager in scene if not referenced
        if (ARPlaneManager == null)
        {
            ARPlaneManager = FindObjectOfType<ARPlaneManager>();  
        }

        if (ARPlaneManager != null)
        {
            // set plane detection flags
            ARPlaneManager.detectionFlags = PlaneTypes;
        }

        // find pointCloudManager in scene if not referenced
        if (ARPointCloudManager == null)
        {
            ARPointCloudManager = FindObjectOfType<ARPointCloudManager>();
        }

        // setup Plane & Cloud Prefabs
        SetupARPrefabs();

        // subscribe to AR Events to track Ready state
        SubscribeAREvents();

        // editor set ready because there are no AR Events triggered
        if (Application.isEditor)
        {
            isTracking = true;
            hasPlanes = true;
            CheckARReadyState();
        }
    }

    protected virtual void SetupARPrefabs()
    {
        if (ARPlaneManager != null)
        {
            // normal plane prefab
            if(!DebugMode && ARPlanePrefab != null)
            {
                ARPlaneManager.planePrefab = ARPlanePrefab;
            }
            // debug plane prefab
            if (DebugMode && ARPlanePrefabDebug != null)
            {
                ARPlaneManager.planePrefab = ARPlanePrefabDebug;
            }
        }

        if (ARPointCloudManager != null)
        {
            // normal point cloud prefab
            if (!DebugMode && ARPointCloudPrefab != null)
            {
                ARPointCloudManager.pointCloudPrefab = ARPointCloudPrefab;
            }
            // debug point cloud prefab
            if (DebugMode && ARPointCloudPrefab != null)
            {
                ARPointCloudManager.pointCloudPrefab = ARPointCloudPrefabDebug;
            }
        }

    }

    protected virtual void Update()
    {
        // reset debug text on every frame
        DebugText.text = "";
        // calc fps in debug mode
        if (DebugMode)
        {
            CalcFramesPerSecond();
        }
    }

    protected void AddDebugLine(string name, string value)
    {
        if (DebugMode)
        {
            DebugText.text += $"\n{name}: {value}";
        }
    }

    protected void CalcFramesPerSecond()
    {
        // calc frames per second
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;

        AddDebugLine("fps", Mathf.Ceil(fps).ToString());
    }

    protected void SubscribeAREvents()
    {
        if (WaitForPlanes)
        {
            ARSubsystemManager.cameraFrameReceived += CheckARPlanes;
        }

        ARSubsystemManager.systemStateChanged += CheckARTrackingState;
    }

    protected void UnsubscribeAREvents()
    {
        if (WaitForPlanes)
        {
            ARSubsystemManager.cameraFrameReceived -= CheckARPlanes;
        }

        // subscribe to AR Session State changed
        ARSubsystemManager.systemStateChanged -= CheckARTrackingState;
    }

    protected void ShowAnimation(Animator animator)
    {
        if (animator != null)
        {
            // activate gameObject
            animator.gameObject.SetActive(true);
            // trigger start
            animator.SetTrigger(ANIM_FADEON);
        }
    }

    protected void HideAnimation(Animator animator)
    {
        if (animator != null)
        {
            // trigger fade of ( is not really shown becasue we set gameobject inactive ) 
            animator.SetTrigger(ANIM_FADEOFF);

            // set animation object inactive to avoid useless animation updates
            animator.gameObject.SetActive(false);
        }
    }

    void CheckARReadyState()
    {
        bool readyState = ARReady;

        if(isTracking)
        {
            // wait for planes if required
            if(!WaitForPlanes || WaitForPlanes && hasPlanes)
            {
                // Hide Phone Animation
                HideAnimation(MovePhoneAnim);

                // set ready flag
                ARReady = true;
            }
           
        }
        else
        {
            // Show Phone Animation
            ShowAnimation(MovePhoneAnim);

            // reset planes flag
            if (WaitForPlanes)
            {
                hasPlanes = false; // force recheck
            }


            // set ready flag
            ARReady = false;
        }

        // check if readyState changed
        if(readyState != ARReady)
        {
            // trigger event if there are subscribers
            ARReadyStateChanged?.Invoke(new ARReadyStateEventArgs
            {
                Ready = ARReady
            });
        }

    }

    void CheckARTrackingState(ARSystemStateChangedEventArgs obj)
    {

        Debug.Log("---> Got SystemStateChanged to: " + obj.state);

        if (obj.state == ARSystemState.SessionTracking)
        {
            isTracking = true;
        }
        else
        {
            isTracking = false;
        }

        CheckARReadyState();
      
    }

    void CheckARPlanes(ARCameraFrameEventArgs obj)
    {
        Debug.Log("---> Got CameraFrameEvent");

        // wait for session ready to check if planes are found & only check if not already found ( to avoid checking on every frame )
        if (isTracking && !hasPlanes)
        {
            List<ARPlane> allPlanes = new List<ARPlane>();
            ARPlaneManager.GetAllPlanes(allPlanes);
            hasPlanes = allPlanes.Count > 0;

            CheckARReadyState();
        }
       
    }

}
