using UnityEngine;

public class LightSource : MonoBehaviour
{
	[SerializeField, Range(0, 1)] private float _lightIntensity = 1f;
	[SerializeField, Range(0, 10)] private float _lightRadius = 5f;

	private Vector3 _lastWorldPosition;

	private void Start()
	{
		_lastWorldPosition = transform.position;
	}

	private void Update()
	{
		if(Lightmap.Instance.GetRenderTexture() == null) return;

		float updateThreshold = 1f / Lightmap.Instance.GetLightmapScale();

		if (Vector3.Distance(transform.position, _lastWorldPosition) >= updateThreshold)
		{
			UpdateLightmap();
			_lastWorldPosition = transform.position;
		}
	}

	private void UpdateLightmap()
	{
		// Implement the logic to update the lightmap
		Debug.Log("Lightmap updated for light source at position: " + transform.position);
		Lightmap.Instance.DispatchComputeShader();
	}

	public float GetIntensity()
	{
		return _lightIntensity;
	}

	public float GetRadius()
	{
		return _lightRadius;
	}
}