using UnityEngine;

public class PlayerArmVisualPreset : MonoBehaviour
{
    // Function to return the Transform of the GameObject this script is attached to
    public Transform GetArmTransform()
    {
        return this.transform;
    }

    // Function to return the Transform of the first child GameObject (child at index 0)
    public Transform GetItemInHandTransform()
    {
        if (transform.childCount > 0)
        {
            return transform.GetChild(0);
        }
        else
        {
            Debug.LogError("No child found! This GameObject has no children.");
            return null;
        }
    }
}