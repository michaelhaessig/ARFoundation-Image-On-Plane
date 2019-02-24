using UnityEngine;
using System.Collections;
using Lean.Touch;

/*
 * @author Michael Hässig
 * @email michael.haessig (at) gmail (dot) com
 */
public class ARImageRotate : LeanRotateCustomAxis
{
    ARImageRotate() : base()
    {
        // set forward vector as axsis
        // this works when placed on a plane with a rotation to only rotate like a clock
        Axis = Vector3.forward;
    }
}
