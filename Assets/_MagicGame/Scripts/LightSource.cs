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
	
	private void OnEnable()
	{
		Lightmap.Instance.RegisterLightSource(this);
	}
	
	private void OnDisable()
	{
		Lightmap.Instance.DeregisterLightSource(this);
	}

	private void Update()
	{
		if(Lightmap.Instance.GetRenderTexture() == null || !WorldManager.Instance.IsTicking()) return;

		float updateThreshold = 1f / Lightmap.Instance.GetLightmapScale();

		if (Vector3.Distance(transform.position, _lastWorldPosition) >= updateThreshold)
		{
			Lightmap.Instance.UpdateLightMap();
			_lastWorldPosition = transform.position;
		}
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