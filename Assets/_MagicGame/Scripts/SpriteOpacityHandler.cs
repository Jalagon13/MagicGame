using UnityEngine;

public class SpriteOpacityHandler : MonoBehaviour
{
    [field: SerializeField] public SpriteRenderer SpriteRenderer { get; private set; }
    
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject.layer == 11 || other.gameObject.layer == 12)
        {
            SetTranslucent();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        SetOpaque();
    }

    private void SetOpaque()
    {
        Color color = SpriteRenderer.color;
        color.a = 1f;
        SpriteRenderer.color = color;
    }

    private void SetTranslucent()
    {
        Color color = SpriteRenderer.color;
        color.a = 0.5f;
        SpriteRenderer.color = color;
    }
}
