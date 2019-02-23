#if UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4
	#define UNITY_OLD_LINE_RENDERER
#endif
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Lean.Touch
{
	// This script will draw the path each finger has taken since it started being pressed
	public class LeanFingerTrail : MonoBehaviour
	{
		// Event signature
		[System.Serializable] public class LeanFingerEvent : UnityEvent<LeanFinger> {}

		// This class will store an association between a Finger and a LineRenderer instance
		[System.Serializable]
		public class Link
		{
			public LeanFinger   Finger; // The finger associated with this link
			public LineRenderer Line; // The LineRenderer instance associated with this link
		}

		[Tooltip("Ignore fingers with StartedOverGui?")]
		public bool IgnoreStartedOverGui = true;

		[Tooltip("Ignore fingers with IsOverGui?")]
		public bool IgnoreIsOverGui;

		[Tooltip("Must RequiredSelectable.IsSelected be true?")]
		public LeanSelectable RequiredSelectable;

		[Tooltip("The line prefab")]
		public LineRenderer LinePrefab;

		[Tooltip("The conversion method used to find a world point from a screen point")]
		public LeanScreenDepth ScreenDepth;

		[Tooltip("The maximum amount of fingers used")]
		public int MaxLines;

		[Tooltip("The camera the translation will be calculated using (default = MainCamera)")]
		public Camera Camera;

		// This stores all the links
		private List<Link> links = new List<Link>();

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
			LeanTouch.OnFingerUp  += FingerUp;
		}

		protected virtual void OnDisable()
		{
			// Unhook events
			LeanTouch.OnFingerSet -= FingerSet;
			LeanTouch.OnFingerUp  -= FingerUp;
		}

		// Override the WritePositions method from LeanDragLine
		protected virtual void WritePositions(LineRenderer line, LeanFinger finger)
		{
			// Reserve one vertex for each snapshot
#if UNITY_OLD_LINE_RENDERER
			line.SetVertexCount(finger.Snapshots.Count);
#else
			line.positionCount = finger.Snapshots.Count;
#endif
			// Loop through all snapshots
			for (var i = 0; i < finger.Snapshots.Count; i++)
			{
				var snapshot = finger.Snapshots[i];

				// Get the world postion of this snapshot
				var worldPoint = ScreenDepth.Convert(snapshot.ScreenPosition, Camera, gameObject);

				// Write position
				line.SetPosition(i, worldPoint);
			}
		}

		private void FingerSet(LeanFinger finger)
		{
			// ignore?
			if (MaxLines > 0 && links.Count >= MaxLines)
			{
				return;
			}

			if (IgnoreStartedOverGui == true && finger.StartedOverGui == true)
			{
				return;
			}

			if (IgnoreIsOverGui == true && finger.IsOverGui == true)
			{
				return;
			}

			if (RequiredSelectable != null && RequiredSelectable.IsSelectedBy(finger) == false)
			{
				return;
			}

			// Get link for this finger and write positions
			var link = FindLink(finger, true);

			if (link != null && link.Line != null)
			{
				WritePositions(link.Line, link.Finger);
			}
		}

		private void FingerUp(LeanFinger finger)
		{
			// Find link for this finger, and clear it
			var link = FindLink(finger, false);

			if (link != null)
			{
				links.Remove(link);

				LinkFingerUp(link);

				if (link.Line != null)
				{
					Destroy(link.Line.gameObject);
				}
			}
		}

		protected virtual void LinkFingerUp(Link link)
		{
		}

		// Searches through all links for the one associated with the input finger
		private Link FindLink(LeanFinger finger, bool createIfNull)
		{
			// Find existing link?
			for (var i = 0; i < links.Count; i++)
			{
				var link = links[i];

				if (link.Finger == finger)
				{
					return link;
				}
			}

			// Make new link?
			if (createIfNull == true)
			{
				var link = new Link();

				link.Finger = finger;
				link.Line   = Instantiate(LinePrefab);

				links.Add(link);

				return link;
			}

			return null;
		}
	}
}