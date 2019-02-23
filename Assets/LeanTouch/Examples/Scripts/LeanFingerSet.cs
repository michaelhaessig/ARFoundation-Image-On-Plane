using UnityEngine;
using UnityEngine.Events;

namespace Lean.Touch
{
	// This script calls the OnFingerSet event while a finger is touching the screen
	public class LeanFingerSet : MonoBehaviour
	{
		public enum DeltaCoordinatesType
		{
			Screen,
			Scaled
		}

		// Event signature
		[System.Serializable] public class LeanFingerEvent : UnityEvent<LeanFinger> {}
		[System.Serializable] public class Vector2Event : UnityEvent<Vector2> {}

		[Tooltip("Ignore fingers with StartedOverGui?")]
		public bool IgnoreStartedOverGui = true;

		[Tooltip("Ignore fingers with IsOverGui?")]
		public bool IgnoreIsOverGui;

		[Tooltip("If the finger didn't move, ignore it?")]
		public bool IgnoreIfStatic;

		[Tooltip("If RequiredSelectable.IsSelected is false, ignore?")]
		public LeanSelectable RequiredSelectable;

		[Tooltip("The coordinate space of the OnSetDelta values")]
		public DeltaCoordinatesType DeltaCoordinates;

		public LeanFingerEvent OnSet;

		public Vector2Event OnSetDelta;

#if UNITY_EDITOR
		protected virtual void Reset()
		{
			Start();
		}
#endif

		protected virtual void Start()
		{
			if (RequiredSelectable == null)
			{
				RequiredSelectable = GetComponent<LeanSelectable>();
			}
		}

		protected virtual void OnEnable()
		{
			// Hook events
			LeanTouch.OnFingerSet += FingerSet;
		}

		protected virtual void OnDisable()
		{
			// Unhook events
			LeanTouch.OnFingerSet -= FingerSet;
		}

		private void FingerSet(LeanFinger finger)
		{
			// Get delta
			var delta = finger.ScreenDelta;

			// Ignore?
			if (IgnoreStartedOverGui == true && finger.StartedOverGui == true)
			{
				return;
			}

			if (IgnoreIsOverGui == true && finger.IsOverGui == true)
			{
				return;
			}

			if (IgnoreIfStatic == true && finger.ScreenDelta.magnitude <= 0.0f)
			{
				return;
			}

			if (RequiredSelectable != null && RequiredSelectable.IsSelected == false)
			{
				return;
			}

			// Scale?
			if (DeltaCoordinates == DeltaCoordinatesType.Scaled)
			{
				delta *= LeanTouch.ScalingFactor;
			}

			// Call event
			if (OnSet != null)
			{
				OnSet.Invoke(finger);
			}

			if (OnSetDelta != null)
			{
				OnSetDelta.Invoke(delta);
			}
		}
	}
}