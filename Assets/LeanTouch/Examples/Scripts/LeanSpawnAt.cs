using UnityEngine;

namespace Lean.Touch
{
	// This component allows you to spawn a prefab at a single point
	public class LeanSpawnAt : MonoBehaviour
	{
		[Tooltip("The prefab that gets spawned")]
		public Transform Prefab;

		[Tooltip("The camera that the prefabs will spawn in front of (None = MainCamera)")]
		public Camera Camera;

		[Tooltip("The conversion method used to find a world point from a screen point")]
		public LeanScreenDepth ScreenDepth;

		public void Spawn(LeanFinger finger)
		{
			if (Prefab != null && finger != null)
			{
				var instance   = Instantiate(Prefab);
				var worldPoint = ScreenDepth.Convert(finger.ScreenPosition, Camera, gameObject);

				instance.position = worldPoint;
				instance.rotation = transform.rotation;
			}
		}
	}
}