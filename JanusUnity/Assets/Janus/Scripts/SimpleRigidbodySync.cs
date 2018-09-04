using UnityEngine;
using Janus;

[AddComponentMenu("Janus/Simple Rigidbody Sync")]
[RequireComponent(typeof(Rigidbody))]
public class SimpleRigidbodySync : MonoBehaviour
{
	public bool HasControl;
	public bool SyncPosition = true;
	public bool SyncRotation = true;
	public float SendRate = 60f;

	public Timeline<Vector3> Position;
	public Timeline<Quaternion> Rotation;

	void FixedUpdate ()
	{
		if (!HasControl && rigidbody.useGravity)
		{
			rigidbody.useGravity = false;
			rigidbody.velocity = Vector3.zero;
		}

		if (Position == null |
			Rotation == null ||
			Position.NumEntries == 0 ||
			Rotation.NumEntries == 0)
			return;

		if (HasControl)
		{
			Position[0] = rigidbody.position;
			Rotation[0] = rigidbody.rotation;
		}
		else
		{
		}
	}
}