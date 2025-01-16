using System;
using UnityEngine;

public class NpcWallCollider : MonoBehaviour
{
	// Define the event
	public event EventHandler<WallCollisionEventArgs> OnWallCollide;
	public class WallCollisionEventArgs : EventArgs
	{
		public Vector2 ContactPoint;
	} 

	private void OnCollisionEnter2D(Collision2D collision)
	{
		if (collision.gameObject.layer == 3) // Assuming layer 3 is the "Wall" layer
		{
			// Pass the point of contact to the event
			Vector2 contactPoint = collision.GetContact(0).point;
			OnWallCollisionStateChanged(contactPoint);
		}
	}

	// Method to invoke the event
	protected virtual void OnWallCollisionStateChanged(Vector2 contactPoint)
	{
		OnWallCollide?.Invoke(this, new WallCollisionEventArgs()
		{
			ContactPoint = contactPoint
		});
	}
}