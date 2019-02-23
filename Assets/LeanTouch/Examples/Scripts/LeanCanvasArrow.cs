using UnityEngine;

namespace Lean.Touch
{
	// This script rotates the current GameObject based on a finger swipe angle
	[ExecuteInEditMode]
	public class LeanCanvasArrow : MonoBehaviour
	{
		[Tooltip("The current angle")]
		public float Angle;

		public void RotateToDelta(Vector2 delta)
		{
			Angle = Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg;
		}

		protected virtual void Update()
		{
			transform.rotation = Quaternion.Euler(0.0f, 0.0f, -Angle);
		}
	}
}