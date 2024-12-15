using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MouseItemUI : MonoBehaviour
{
	[SerializeField] private Image _itemImage;
	[SerializeField] private TextMeshProUGUI _itemQuantityText;
	
	private void Awake()
	{
		_itemImage.color = new Vector4(1, 1, 1, 0);
		_itemImage.sprite = null;
		_itemQuantityText.text = string.Empty;
	}
	
	private void Start()
	{
		InventoryManager.Instance.OnMouseItemUpdated += InventoryManager_OnMouseItemUpdated;
	}

	private void InventoryManager_OnMouseItemUpdated(object sender, InventoryManager.OnMouseItemUpdatedEventArgs e)
	{
		UpdateView(e.MouseItem);
	}

	private void Update()
	{
		if(Camera.main == null) return;
		
		UpdatePosition();
	}
	
	private void UpdatePosition()
	{
		transform.position = Camera.main.WorldToScreenPoint(ActionManager.MouseWorldPosition);
	}
	
	public void UpdateView(InventoryItem item)
	{
		if(item.Item != null)
		{
			_itemImage.color = new Vector4(1, 1, 1, 1);
			_itemImage.sprite = item.Item.UiDisplay;
		}
		else
		{
			_itemImage.color = new Vector4(1, 1, 1, 0);
			_itemImage.sprite = null;
		}
		
		_itemQuantityText.text = item.Item != null ? item.Quantity.ToString() : string.Empty;
	}
	
	private void OnDestroy()
	{
		InventoryManager.Instance.OnMouseItemUpdated -= InventoryManager_OnMouseItemUpdated;
	}
}
