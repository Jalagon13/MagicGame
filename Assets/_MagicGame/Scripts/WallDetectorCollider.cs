using System;
using UnityEngine;

public class WallDetectorCollider : MonoBehaviour
{
	// Define the event
	public event EventHandler<WallCollisionEventArgs> OnWallCollide;
	public class WallCollisionEventArgs : EventArgs
	{
		public Vector2 ContactPoint;
	} 
	
	private EnvironmentID _colliderEnvironment;
	private Collider2D _wallDetectorCollider;
	
	private void Awake()
	{
		_wallDetectorCollider = GetComponent<Collider2D>();
	}
	
	private void Start()
	{
		Pathfinding.Instance.OnPathfindingTilemapCreated += UpdateCollisions;
	}

	private void UpdateCollisions(object sender, Pathfinding.PathfindingTilemapEventArgs e)
	{
		if(e.Environment != _colliderEnvironment)
		{
			Debug.Log(transform.root.name + " Ignoring detection of " + e.TilemapCollider.name);
			Physics2D.IgnoreCollision(_wallDetectorCollider, e.TilemapCollider);
		}
	}

	public void SetEnvironment(EnvironmentID environment) // Sets the environment whose walls this collider will detect
	{
		_colliderEnvironment = environment;
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
	
	private void OnDestroy()
	{
		Pathfinding.Instance.OnPathfindingTilemapCreated -= UpdateCollisions;
	}
}