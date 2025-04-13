using System;
using System.Collections;
using AdvancedTooltips.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TileFocusUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [field: SerializeField] public Sprite FocusWallSprite { get; private set; }
    [field: SerializeField] public Sprite FocusFloorSprite { get; private set; }
    
    private Image _image;
    
    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    private void Start()
    {
        GameInput.Instance.OnMiningFocusToggled += UpdateUI;
    }

    private void UpdateUI(object sender, EventArgs e)
    {
        StartCoroutine(FrameDelay());
    }

    private IEnumerator FrameDelay()
    {
        yield return new WaitForEndOfFrame();
        
        _image.sprite = MiningHandler.FocusingOnWall ? FocusWallSprite : FocusFloorSprite;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Tooltip.ShowNew();
        
        string text = MiningHandler.FocusingOnWall ? "Prioritizing Wall Tiles" : "Prioritizing Floor Tiles";
        Tooltip.JustText(text, Color.white, fontSize: 12f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Tooltip.HideUI();
    }
    
    private void OnDestroy()
    {
        GameInput.Instance.OnMiningFocusToggled -= UpdateUI;
    }
}
