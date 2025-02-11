using System;
using MoreMountains.Tools;
using TMPro;
using UnityEngine;

public class PlayerTimeUI : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI _amountText;
	
	private void Start()
	{
		WorldManager.Instance.OnTick += UpdateTimeUI;
	}

	private void UpdateTimeUI(object sender, WorldManager.OnTickEventArgs e)
	{
		_amountText.text = $"Time:<br> {Mathf.RoundToInt(e.CurrentTime)}/{Mathf.RoundToInt(e.DayDuration)}";
	}
	
	private void OnDestroy()
	{
		WorldManager.Instance.OnTick -= UpdateTimeUI;
	}
}
