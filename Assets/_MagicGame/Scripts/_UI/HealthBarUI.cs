using System;
using MoreMountains.Tools;
using Unity.Netcode;
using UnityEngine;

public class HealthBarUI : MonoBehaviour
{
	[field: SerializeField] public NetworkHealthState NetworkHealthState { get; private set; }
	
	private MMProgressBar _progressBar;
	
	private void Awake()
	{
		_progressBar = GetComponent<MMProgressBar>();
		if (NetworkHealthState == null)
		{
			Debug.LogError("Root GameObject" + transform.root.gameObject.name + " does not have component that implements IHasHealth");
		}
	}
	
	private void Start()
	{
		_progressBar.UpdateBar(1, 0, 1);
		NetworkHealthState.OnHitPointsChanged += UpdateHealthBar;

		Hide();
	}

    private void OnDestroy()
    {
		NetworkHealthState.OnHitPointsChanged -= UpdateHealthBar;
	}

    private void UpdateHealthBar(object sender, PointsChangedEventArgs e)
    {
		_progressBar.UpdateBar(e.CurrentPoints, 0, e.MaxPoints);

		if (e.CurrentPoints >= e.MaxPoints)
		{
			Hide();
		}
		else
		{
			Show();
		}
	}

    private void Show()
	{
		gameObject.SetActive(true);
	}
	
	private void Hide()
	{
		gameObject.SetActive(false);
	}
}
