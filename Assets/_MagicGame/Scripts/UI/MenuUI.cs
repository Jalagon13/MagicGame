using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class MenuUI : MonoBehaviour
{
    private void Start()
    {
        GameInput.Instance.OnInventoryToggle += GameInput_OnInventoryToggle;
		
        Hide();
    }

    private void GameInput_OnInventoryToggle(object sender, GameInput.OnToggleInventoryEventArgs e)
    {
        if(e.InventoryOpen)
        {
            Show();
        }
        else
        {
            Hide();
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
	
    private void OnDestroy()
    {
        GameInput.Instance.OnInventoryToggle -= GameInput_OnInventoryToggle;
    }
}
