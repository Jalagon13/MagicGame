using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public static class Pointer
{
    // gets data on raycast of all gameobject's layers and returns true if finds a gameobject of layerNum
    public static bool IsOverUI ()
    {
        PointerEventData eventDataCurrentPosition = new(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

        foreach (RaycastResult raycastResult in results)
        {
            if (raycastResult.gameObject.layer == 5)
                return true;
        }

        return false;
    }
}

