using System;
using UnityEngine;

public class LightSource : MonoBehaviour
{
	[field: SerializeField, Range(0, 1)] public float LightIntensity { get; private set; } = 1f;
	[field: SerializeField, Range(0, 10)] public float LightRadius { get; private set; }  = 5f;
	
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
		if(Lightmap.Instance.GetRenderTexture() == null || !GameWorld.Instance.IsTicking()) return;

		float updateThreshold = 1f / Lightmap.Instance.GetLightmapScale();

		if (Vector3.Distance(transform.position, _lastWorldPosition) >= updateThreshold)
		{
			Lightmap.Instance.UpdateLightMap();
			_lastWorldPosition = transform.position;
		}
	}
}