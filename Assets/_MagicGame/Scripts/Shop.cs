using System;
using System.Collections.Generic;
using UnityEngine;

public class Shop : MonoBehaviour
{
    [field: SerializeField] public WorldInput WorldInput { get; private set; }
    [field: SerializeField] public List<ItemSO> ItemsToSell { get; private set; }
    
    private void Start()
    {
        GameInput.Instance.OnSecondaryActionStarted += GameInput_OnSecondaryActionStarted;
    }

    private void GameInput_OnSecondaryActionStarted(object sender, EventArgs e)
    {
        var centerPosition = new Vector2(transform.position.x, transform.position.y + 0.5f);
        var playerInRange = Vector2.Distance(Player.LocalClientInstance.transform.position, centerPosition) <= WorldObject.ChestOpenDistance;

        if (WorldInput.IsMouseOverIndputDetector() && playerInRange)
        {
            InGameMenu.Instance.OpenNpcMenu(ItemsToSell, gameObject);
        }
    }

    private void OnDestroy()
    {
        GameInput.Instance.OnSecondaryActionStarted -= GameInput_OnSecondaryActionStarted;
    }
}
