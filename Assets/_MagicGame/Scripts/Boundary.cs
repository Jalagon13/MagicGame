using System.Collections;
using UnityEngine;

public class Boundary : MonoBehaviour
{
    private PolygonCollider2D _boundaryCollider;
	
    private EdgeCollider2D _playerBoundaryCollider;

    private void Awake()
    {
        _boundaryCollider = GetComponent<PolygonCollider2D>();
        _playerBoundaryCollider = GetComponent<EdgeCollider2D>();
    }
	
    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();
		
        GenerateWorldBoundary();
    }

    private void GenerateWorldBoundary()
    {
        Vector2 worldSize = new Vector2(256, 256); // Change hard coded 256 to something else in the future prolly
		
        // Define the corners of the world
        Vector2[] points = new Vector2[5];
        points[0] = new Vector2(0, 0); // Bottom-left
        points[1] = new Vector2(worldSize.x, 0); // Bottom-right
        points[2] = new Vector2(worldSize.x, worldSize.y); // Top-right
        points[3] = new Vector2(0, worldSize.y); // Top-left
        points[4] = points[0]; // Closing the loop
		
        // Set boundary points to camera and player collider
        _boundaryCollider.points = points;
        _playerBoundaryCollider.points = points;
		
        // Send boundary to virtual camera
        // Signal signal = GameSignals.UPDATE_SCENE_BOUNDARY;
        // signal.ClearParameters();
        // signal.AddParameter("Collider", _boundaryCollider);
        // signal.Dispatch();
    }
}
