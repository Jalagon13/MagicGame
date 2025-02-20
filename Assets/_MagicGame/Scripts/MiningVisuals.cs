using System.Collections;
using UnityEngine;

public class MiningVisuals : MonoBehaviour
{
	[SerializeField] private float _duration;
	[Range(0, 1f), SerializeField] private float _startingOpacity;
	
	private LineRenderer _miningVisualRenderer;
	private Color _startColor;
	private float _timer;

	private void Awake()
	{
		_miningVisualRenderer = GetComponent<LineRenderer>();
	}

	private IEnumerator Start()
	{
		_miningVisualRenderer.positionCount = 2;
		_miningVisualRenderer.SetPosition(0, Player.LocalClientInstance.ProjectileSpawnPointTf.position);
		_miningVisualRenderer.SetPosition(1, ActionManager.MouseWorldPosition);

		_startColor = _miningVisualRenderer.startColor;

		// Wait for the duration, then destroy
		yield return new WaitForSeconds(_duration);
		Destroy(gameObject);
	}

	private void Update()
	{
		_timer += Time.deltaTime;
		float fadeProgress = _timer / _duration;

		// Fade out the line renderer
		Color fadedColor = new Color(_startColor.r, _startColor.g, _startColor.b, Mathf.Lerp(_startingOpacity, 0f, fadeProgress));
		_miningVisualRenderer.startColor = fadedColor;
		_miningVisualRenderer.endColor = fadedColor;
	}
}