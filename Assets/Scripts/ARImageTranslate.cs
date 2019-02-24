using UnityEngine;
using System.Collections;
using Lean.Touch;

/*
 * Custom Translate based on the transform direction vectors to keep axis aligned
 * 
 * @author Michael Hässig
 * @email michael.haessig (at) gmail (dot) com
 */
public class ARImageTranslate : LeanTranslate
{
    protected override void Translate(Vector2 screenDelta)
    {
        // Make sure the camera exists
        var camera = LeanTouch.GetCamera(Camera, gameObject);

        if (camera != null)
        {
            // Store old position
            var oldPosition = transform.position;
            // Screen position of the transform
            var screenPosition = camera.WorldToScreenPoint(oldPosition);
            // Add the deltaPosition
            screenPosition += (Vector3)screenDelta;
            // Convert back to world space
            var newPosition = camera.ScreenToWorldPoint(screenPosition);

            // we use the difference as a multiplier and move with the transform direction vectors 
            // the movement is still not perfect .. but it keeps the right axis , maybe there is a better way to match the center of the image with the position based on the input screen touch without loosing the axis 

            var difference = newPosition - oldPosition;
            var distanceInX = Mathf.Abs(difference.x);
            var distanceInY = Mathf.Abs(difference.y);

            // store image rotation
            var imageRotation = transform.rotation;

            // set image rotation with z set to zero ( IMPORTANT: this is needed for the transform.up/right point up/right in world space without the rotation of the image around the z axis )
            transform.rotation = Quaternion.Euler(imageRotation.eulerAngles.x, imageRotation.eulerAngles.y, 0);

            if (screenDelta.x > 0)
            {
                transform.position += (transform.right * distanceInX);
                // transform.Translate(Vector3.right * distanceInX); same effect as above
            }

            if (screenDelta.x < 0)
            {
                transform.position += (-transform.right * distanceInX);
                //transform.Translate(-Vector3.right * distanceInX); same effect as above
            }

            if (screenDelta.y > 0)
            {
                transform.position += (transform.up * distanceInY);
                // transform.Translate(Vector3.up * distanceInY); same effect as above
            }

            if (screenDelta.y < 0)
            {
                transform.position += (-transform.up * distanceInY);
                // transform.Translate(-Vector3.up * distanceInY); same effect as above
            }

            // restore image rotation
            transform.rotation = imageRotation;
        }
    }
}