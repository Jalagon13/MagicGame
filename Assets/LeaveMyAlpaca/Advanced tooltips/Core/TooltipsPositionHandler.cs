namespace AdvancedTooltips.Core
{
    using UnityEngine;

    public class TooltipsPositionHandler : MonoBehaviour
    {
        [SerializeField] internal RectTransform Canvas;
        [SerializeField, Tooltip("should be the same one as in the TooltipReferenceHolder")] internal RectTransform Layout;
        void Update()
        {
            // moves to clamped position of mouse  
            Vector2 anchorPoint = Input.mousePosition / Canvas.localScale.x;

            if (anchorPoint.x + Layout.rect.width > Canvas.rect.width)
                anchorPoint.x = Canvas.rect.width - Layout.rect.width;

            if (anchorPoint.y + Layout.rect.height > Canvas.rect.height)
                anchorPoint.y = Canvas.rect.height - Layout.rect.height;


            Layout.anchoredPosition = anchorPoint;
        }
    }
}