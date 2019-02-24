using UnityEngine;
using System.Collections;

/*
 * @author Michael Hässig
 * @email michael.haessig (at) gmail (dot) com
 */
public class ARImageRemoveIcon : MonoBehaviour
{
    public GameObject followImage;

    // late Update after other update 
    void LateUpdate()
    {
        if(followImage != null)
        {
            transform.position = new Vector3(followImage.transform.position.x, followImage.transform.position.y, followImage.transform.position.z - 0.01F); // overlay z

            // store image rotation
            var imageRotation = followImage.transform.rotation;

            // set rotation 
            transform.rotation = imageRotation;

            // rest image rotation ( IMPORTANT: this is needed to get the normal bounds from the renderer )
            followImage.transform.rotation = Quaternion.identity;

            /* get renderer to find dimension and set icon to the top right in world position */
            var imageRenderer = followImage.GetComponent<Renderer>();

            //Debug.Log($"---> got renderer bounds x: {imageRenderer.bounds.size.x} y: {imageRenderer.bounds.size.y}");
            //Debug.Log($"---> got renderer extends x: {imageRenderer.bounds.extents.x} y: {imageRenderer.bounds.extents.y}");

            // translate current postion ( center of followImage ) to top right  , extends is half the size 
            transform.Translate(new Vector3(imageRenderer.bounds.extents.x, imageRenderer.bounds.extents.y, 0));

            // rest image rotation
            followImage.transform.rotation = imageRotation;
        }
    }

}
