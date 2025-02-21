namespace AdvancedTooltips.Core
{
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// There should be only one of this script on scene
    /// </summary>
    [RequireComponent(typeof(TooltipsInstantiateHandler)), RequireComponent(typeof(TooltipsPositionHandler), typeof(TooltipAnimations))]
    public class TooltipReferenceHolder : MonoBehaviour
    {

        private void Start()
        {
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            TooltipsStatic.referenceHolder = this;
            animations = GetComponent<TooltipAnimations>();
        }


        private TooltipAnimations animations;
        [SerializeField] private float tooltipDelay = .3f;
        /// <summary>
        /// this is where you instantiate all of the prefabs
        /// </summary>
        public VerticalLayoutGroup layout;
        [HideInInspector] public List<GameObject> oldPrefabs = new();
        /// <summary>
        ///  has just text and image
        /// </summary>
        public GameObject JustTextPrefab;
        public TMP_FontAsset defaultFont;

        public GameObject buildingPrefab;

        [Header("Background")]
        public Image background;
        public Sprite defaultBackgroundSprite;
        public Color defaultBackgroundColor;
        public RectOffset defaultPadding;



        bool turnOn = true;
        public void ShowUI()
        {
            Invoke(nameof(TurnOn), tooltipDelay);
            turnOn = true;
        }
        public void HideUI()
        {
            animations.HideAnimation();
            turnOn = false;
        }

        // its used to delay showing up of tooltip
        private void TurnOn()
        {
            if (!turnOn)
                return;

            animations.ShowUpAnimation();

        }
        public void ClearOldPrefabs()
        {
            for (int i = 0; i < oldPrefabs.Count; i++)
            {
                Destroy(oldPrefabs[i]);
            }
            oldPrefabs.Clear();
        }

    }
}