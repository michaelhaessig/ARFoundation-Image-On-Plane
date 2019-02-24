using UnityEngine;
using Lean.Touch;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using UnityEngine.Experimental.XR;

/*
 * @author Michael Hässig
 * @email michael.haessig (at) gmail (dot) com
 */
public class ARPlaceImageOnPlane : ARImageScene {

    public LayerMask DevLayerMask;

    private bool imagePlaced = false;

    private TrackableId placedPlaneId = TrackableId.InvalidId;

    private float lerpSpeed = 6.0f;

    static List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();

    ARPlaceImageOnPlane() : base()
    {
        // Only use Vertical Planes ( walls )
        PlaneTypes = UnityEngine.XR.ARExtensions.PlaneDetectionFlags.Vertical;
    }

    protected void PlaceOnHit(ARRaycastHit hit)
    {
        Pose hitPose = hit.pose;

        // store plane id
        placedPlaneId = hit.trackableId;

        AddDebugLine("hit rotation", hit.pose.rotation.eulerAngles.ToString());

        AddDebugLine("plane id", placedPlaneId.ToString());

        // position image 1cm above hit
        Vector3 newPosition = hitPose.position + hitPose.up * 0.01f;

        // smoth position transition with lerp 
        imageInRoom.transform.position = Vector3.Lerp(imageInRoom.transform.position, newPosition, Time.deltaTime * lerpSpeed);

        // find hit plane
        ARPlane arPlane = ARPlaneManager.TryGetPlane(placedPlaneId);

        AddDebugLine("plane Alignment", arPlane.boundedPlane.Alignment.ToString());

        // alignment check ( we only enabled Vertical )
        if (arPlane.boundedPlane.Alignment != PlaneAlignment.Horizontal)  // We assume everything that is not horizontal is vertical ( IOS sometimes reports NonAxsis for Vertical )
        {
            // set transform eulerAngles with the y transform of the plane ( not the hit )


            // android needs 90 degrees modifications
            if (Application.platform == RuntimePlatform.Android)
            {
                // z = 270 indicates plane is flipped
                if (System.Math.Abs(arPlane.transform.eulerAngles.z - 270) < 0.1) // float == check with 0.1 tolerance
                {
                    imageInRoom.transform.eulerAngles = new Vector3(0, arPlane.transform.eulerAngles.y - 90, 0);
                }
                else
                {
                    imageInRoom.transform.eulerAngles = new Vector3(0, arPlane.transform.eulerAngles.y + 90, 0);
                }
            }

            // ios need 180 degree modification 
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                // z = 180 indiccates plane is flipped
                if (System.Math.Abs(arPlane.transform.eulerAngles.z - 180) < 0.1) // float == check with 0.1 tolerance
                {
                    imageInRoom.transform.eulerAngles = new Vector3(0, arPlane.transform.eulerAngles.y - 180, 0);
                }
                else
                {
                    imageInRoom.transform.eulerAngles = new Vector3(0, arPlane.transform.eulerAngles.y, 0); // no y modification needed
                }
            }

        }
        // assume horizontal ( Floor ) 
        else
        {
            // set transform eulerAngles with the y transform of the plane ( not the hit )
            // lay image flat on the ground
            imageInRoom.transform.eulerAngles = new Vector3(90, arPlane.transform.eulerAngles.y, 90);
        }
    }

    protected override void Update()
    {
        base.Update();

        // try place image if not placed
        if (!imagePlaced)
        {
            // tracking and image data has to be ready
            if (ARReady && DataReady)
            {
                // calculate screen center  ( we recalc each frame to support landscape/portrait changes )
                var screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);

                if (!Application.isEditor)
                {

                    // check if we point at the inside of a planes bounds
                    if (ARSessionOrigin.Raycast(screenCenter, raycastHits, TrackableType.PlaneWithinBounds)) 
                    {
                        AddDebugLine("plane TrackableType", "WithinBounds");

                        // place on the first hit
                        PlaceOnHit(raycastHits[0]);

                        // set active if not already
                        if (!imageInRoom.activeSelf)
                        {
                            imageInRoom.SetActive(true);
                        }
                    }
                    // no direct plane found but image was placed on a Plane before
                    else if(placedPlaneId != TrackableId.InvalidId)
                    {
                        // try raycast to plane with infinity bounds ( this allows to build a endless wall with the world position of the plane )
                        if (ARSessionOrigin.Raycast(screenCenter, raycastHits, TrackableType.PlaneWithinInfinity))
                        {                        
                            AddDebugLine("plane TrackableType", "WithinInfinity");

                            // find the placed plane via the id
                            for (int i = 0; i < raycastHits.Count; i++)
                            {
                                var hit = raycastHits[i];

                                if(hit.trackableId == placedPlaneId)
                                {
                                    // place on the hit
                                    PlaceOnHit(raycastHits[0]);
                                }
                            }

                        }

                    }


                } 
                else // Editor Dev
                {
                    Ray ray = Camera.main.ScreenPointToRay(new Vector3(Camera.main.pixelWidth * 0.5f, Camera.main.pixelHeight * 0.5f, 0f));
                    RaycastHit hit;

                    if (Physics.Raycast(ray, out hit, 500f, DevLayerMask)) // ARDev layer
                    {
                        imageInRoom.transform.position = hit.point + hit.transform.forward * -0.01f;
                        imageInRoom.transform.rotation = hit.transform.rotation;

                 
                        // set active if not already
                        if (!imageInRoom.activeSelf)
                        {
                            imageInRoom.SetActive(true);
                        }
                    }
                    else
                    {
                        // no hit hide image 
                        imageInRoom.SetActive(false);
                    }

                }

            }
        }

        if(imageInRoom != null)
        {
            AddDebugLine("position", imageInRoom.transform.position.ToString());
            AddDebugLine("rotation", imageInRoom.transform.eulerAngles.ToString());
        }
    }

    // Event when finger is tapped on screen and AR tracking is ready
    protected override void ARReadyFingerTap(LeanFinger finger)
    {
        base.ARReadyFingerTap(finger);

        // set image as placed
        imagePlaced = true;

        // hide tracked planes
        ARSessionOrigin.trackablesParent.gameObject.SetActive(false);
    }


    protected override void OnRemoveIconClicked()
    {
        base.OnRemoveIconClicked();

        // reset placed to allow new placement 
        imagePlaced = false;
        placedPlaneId = TrackableId.InvalidId;

        // show tracked planes
        ARSessionOrigin.trackablesParent.gameObject.SetActive(true);
    }


    protected override void OnARReadyStateChanged(ARReadyStateEventArgs obj)
    {
        base.OnARReadyStateChanged(obj);

        if (!obj.Ready)
        {
            if (imageInRoom != null)
            {
                // hide the image ( NEW: avoid hiding when tracking is lost ) 
                // imageInRoom.SetActive(false);
            }
        }
    }
}
