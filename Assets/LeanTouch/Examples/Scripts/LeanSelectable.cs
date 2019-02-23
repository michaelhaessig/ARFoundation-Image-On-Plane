using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

namespace Lean.Touch
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(LeanSelectable))]
	public class LeanSelectable_Editor : Editor
	{
		private bool showEvents;

		// Draw the whole inspector
		public override void OnInspectorGUI()
		{
			EditorGUI.BeginDisabledGroup(true);
				DrawDefault("isSelected");
			EditorGUI.EndDisabledGroup();
			DrawDefault("DeselectOnUp");
			DrawDefault("HideWithFinger");
			DrawDefault("IsolateSelectingFingers");

			EditorGUILayout.Separator();

			showEvents = EditorGUILayout.Foldout(showEvents, "Show Events");

			if (showEvents == true)
			{
				DrawDefault("OnSelect");
				DrawDefault("OnSelectSet");
				DrawDefault("OnSelectUp");
				DrawDefault("OnDeselect");
			}
		}

		private void DrawDefault(string name)
		{
			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(serializedObject.FindProperty(name));

			if (EditorGUI.EndChangeCheck() == true)
			{
				serializedObject.ApplyModifiedProperties();
			}
		}
	}
}
#endif

namespace Lean.Touch
{
	// This component makes this GameObject selectable
	// To then select it, you can use LeanSelect and trigger the selection from LeanFingerTap or similar
	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	public class LeanSelectable : MonoBehaviour
	{
		// Event signature
		[System.Serializable] public class LeanFingerEvent : UnityEvent<LeanFinger> {}

		public static List<LeanSelectable> Instances = new List<LeanSelectable>();

		[Tooltip("Should this get deselected when the selecting finger goes up?")]
		public bool DeselectOnUp;

		[Tooltip("Should IsSelected temporarily return false if the selecting finger is still being held? This is useful when selecting multiple objects using a complex gesture (e.g. RTS style selection box)")]
		public bool HideWithFinger;

		[Tooltip("If the selecting fingers are still active, only return those to RequiredSelectable queries?")]
		public bool IsolateSelectingFingers;

		// Returns isSelected, or false if HideWithFinger is true and SelectingFinger is still set
		public bool IsSelected
		{
			get
			{
				// Hide IsSelected?
				if (HideWithFinger == true && isSelected == true && selectingFingers.Count > 0)
				{
					return false;
				}

				return isSelected;
			}
		}

		// Bypass HideWithFinger
		public bool IsSelectedRaw
		{
			get
			{
				return isSelected;
			}
		}

		// The amount of LeanSelectables with IsSelected == true
		public static int IsSelectedCount
		{
			get
			{
				var count = 0;

				for (var i = Instances.Count - 1; i >= 0; i--)
				{
					if (Instances[i].IsSelected == true)
					{
						count += 1;
					}
				}

				return count;
			}
		}

		// Called when selection begins (finger = the finger that selected this)
		public LeanFingerEvent OnSelect;

		// Called every frame this selectable is selected with a finger (finger = the finger that selected this)
		public LeanFingerEvent OnSelectSet;

		// Called when the selecting finger goes up (finger = the finger that selected this)
		public LeanFingerEvent OnSelectUp;

		// Called when this is deselected, if OnSelectUp hasn't been called yet, it will get called first
		public UnityEvent OnDeselect;

		// Is this selectable selected?
		[Tooltip("If you want to change this, do it via the Select/Deselect methods (accessible from the context menu gear icon in editor)")]
		[SerializeField]
		private bool isSelected;

		// The fingers that were used to select this GameObject
		// If a finger goes up then it will be removed from this list
		[System.NonSerialized]
		private List<LeanFinger> selectingFingers = new List<LeanFinger>();

		public LeanFinger SelectingFinger
		{
			get
			{
				if (selectingFingers.Count > 0)
				{
					return selectingFingers[0];
				}

				return null;
			}
		}

		public List<LeanFinger> SelectingFingers
		{
			get
			{
				return selectingFingers;
			}
		}

		/// <summary>If requiredSelectable is set and not selected, the fingers list will be empty. If selected then the fingers list will only contain the selecting finger.</summary>
		public static List<LeanFinger> GetFingers(bool ignoreIfStartedOverGui, bool ignoreIfOverGui, int requiredFingerCount = 0, LeanSelectable requiredSelectable = null)
		{
			var fingers = LeanTouch.GetFingers(ignoreIfStartedOverGui, ignoreIfOverGui, requiredFingerCount);

			if (requiredSelectable != null)
			{
				if (requiredSelectable.IsSelected == false)
				{
					fingers.Clear();
				}

				if (requiredSelectable.IsolateSelectingFingers == true && requiredSelectable.selectingFingers.Count > 0)
				{
					fingers.Clear();

					fingers.AddRange(requiredSelectable.selectingFingers);
				}
			}

			return fingers;
		}

		/// <summary>If requiredSelectable is set and not selected, the fingers list will be empty.</summary>
		public static List<LeanFinger> GetFingersOrClear(bool ignoreIfStartedOverGui, bool ignoreIfOverGui, int requiredFingerCount = 0, LeanSelectable requiredSelectable = null)
		{
			var fingers = LeanTouch.GetFingers(ignoreIfStartedOverGui, ignoreIfOverGui, requiredFingerCount);

			if (requiredSelectable != null && requiredSelectable.IsSelected == false)
			{
				fingers.Clear();
			}

			return fingers;
		}

		// Limit the amount of selected objects
		public static void Cull(int maxCount)
		{
			var count = 0;

			for (var i = Instances.Count - 1; i >= 0; i--)
			{
				var selectable = Instances[i];

				if (selectable.IsSelected == true)
				{
					count += 1;

					if (count > maxCount)
					{
						selectable.Deselect();
					}
				}
			}
		}

		// Returns the selectable that was selected, or null
		public static LeanSelectable FindSelectable(LeanFinger finger)
		{
			for (var i = Instances.Count - 1; i >= 0; i--)
			{
				var selectable = Instances[i];

				if (selectable.IsSelectedBy(finger) == true)
				{
					return selectable;
				}
			}

			return null;
		}

		// Replace all currently selected selectables with this new list
		public static void ReplaceSelection(LeanFinger finger, List<LeanSelectable> selectables)
		{
			var selectableCount = 0;

			// Deselect missing selectables
			if (selectables != null)
			{
				for (var i = Instances.Count - 1; i >= 0; i--)
				{
					var selectable = Instances[i];

					if (selectable.isSelected == true && selectables.Contains(selectable) == false)
					{
						selectable.Deselect();
					}
				}
			}

			// Add new selectables
			if (selectables != null)
			{
				for (var i = selectables.Count - 1; i >= 0; i--)
				{
					var selectable = selectables[i];

					if (selectable != null)
					{
						if (selectable.isSelected == false)
						{
							selectable.Select(finger);
						}

						selectableCount += 1;
					}
				}
			}

			// Nothing was selected?
			if (selectableCount == 0)
			{
				DeselectAll();
			}
		}

		public bool IsSelectedBy(LeanFinger finger)
		{
			for (var i = selectingFingers.Count - 1; i >= 0; i--)
			{
				if (selectingFingers[i] == finger)
				{
					return true;
				}
			}

			return false;
		}

		public bool GetIsSelected(bool raw)
		{
			return raw == true ? IsSelectedRaw : IsSelected;
		}

		[ContextMenu("Select")]
		public void Select()
		{
			Select(null);
		}

		public static void DeselectAll()
		{
			for (var i = Instances.Count - 1; i >= 0; i--)
			{
				Instances[i].Deselect();
			}
		}

		// NOTE: Multiple selection is allowed to allow reselection, and updating of the selecting finger
		public void Select(LeanFinger finger)
		{
			isSelected = true;

			if (finger != null)
			{
				if (IsSelectedBy(finger) == false)
				{
					selectingFingers.Add(finger);
				}
			}

			if (OnSelect != null)
			{
				OnSelect.Invoke(finger);
			}

			// Make sure FingerUp is only registered once
			LeanTouch.OnFingerUp -= FingerUp;
			LeanTouch.OnFingerUp += FingerUp;

			// Make sure FingerSet is only registered once
			LeanTouch.OnFingerSet -= FingerSet;
			LeanTouch.OnFingerSet += FingerSet;
		}

		[ContextMenu("Deselect")]
		public void Deselect()
		{
			// Make sure we don't deselect multiple times
			if (isSelected == true)
			{
				isSelected = false;

				for (var i = selectingFingers.Count - 1; i >= 0; i--)
				{
					var selectingFinger = selectingFingers[i];

					if (OnSelectUp != null && selectingFinger != null)
					{
						OnSelectUp.Invoke(selectingFinger);
					}
				}

				selectingFingers.Clear();

				if (OnDeselect != null)
				{
					OnDeselect.Invoke();
				}
			}
		}

		protected virtual void OnEnable()
		{
			// Register instance
			Instances.Add(this);
		}

		protected virtual void OnDisable()
		{
			// Unregister instance
			Instances.Remove(this);

			if (isSelected == true)
			{
				Deselect();
			}
		}

		protected virtual void LateUpdate()
		{
			// Null the selecting finger?
			// NOTE: This is done in LateUpdate so certain OnFingerUp actions that require checking SelectingFinger can still work properly
			for (var i = selectingFingers.Count - 1; i >= 0; i--)
			{
				var selectingFinger = selectingFingers[i];

				if (selectingFinger.Set == false || isSelected == false)
				{
					selectingFingers.RemoveAt(i);
				}
			}
		}

		private static void FingerSet(LeanFinger finger)
		{
			// Loop through all selectables
			for (var i = Instances.Count - 1; i >= 0; i--)
			{
				var selectable = Instances[i];

				// Was this selected with this finger?
				if (selectable.IsSelectedBy(finger) == true)
				{
					if (selectable.OnSelectSet != null)
					{
						selectable.OnSelectSet.Invoke(finger);
					}
				}
			}
		}

		private static void FingerUp(LeanFinger finger)
		{
			// Loop through all selectables
			for (var i = Instances.Count - 1; i >= 0; i--)
			{
				var selectable = Instances[i];

				// Was this selected with this finger?
				for (var j = selectable.selectingFingers.Count - 1; j >= 0; j--)
				{
					if (selectable.selectingFingers[j] == finger)
					{
						if (selectable.DeselectOnUp == true && selectable.IsSelected == true)
						{
							selectable.Deselect();
						}
						// Deselection will call OnSelectUp
						else
						{
							// Null the finger and call OnSelectUp
							selectable.selectingFingers.RemoveAt(j);

							if (selectable.OnSelectUp != null)
							{
								selectable.OnSelectUp.Invoke(finger);
							}
						}
					}
				}
			}
		}
	}
}