using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEditor;


namespace ProjectTinker
{
	public class PlayerSpriteOverrider : NetworkBehaviour
	{
	    [field: SerializeField]
	    public ArmorType ArmorType { get; private set; }
    
	    [SerializeField] 
	    private bool _isArmSprite;

	    [SerializeField, Tooltip("Renderer for armor overlay (if used)")]
	    private SpriteRenderer _overlaySpriteRenderer;

	    private NetworkVariable<ushort> _armorEquippedId = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	    private Dictionary<string, Sprite> _spriteSheetLookup;
	    private SpriteRenderer _playerPartRenderer;
	    private Player _thisPlayer;

	    private void Awake()
	    {
	        _playerPartRenderer = GetComponent<SpriteRenderer>();

	        if (_thisPlayer == null)
	        {
	            _thisPlayer = transform.root.GetComponent<Player>();
	        }

	        if (_thisPlayer.OwnerClientId == NetworkManager.LocalClientId)
	        {
	            ArmorSlotUI.OnArmorUpdated += OnArmorUpdated;
	        }

	        _armorEquippedId.OnValueChanged += OnArmorEquippedIdChanged;
	    }

	    public override void OnDestroy()
	    {
	        if (_thisPlayer.OwnerClientId == NetworkManager.LocalClientId)
	        {
	            ArmorSlotUI.OnArmorUpdated -= OnArmorUpdated;
	        }

	        _armorEquippedId.OnValueChanged -= OnArmorEquippedIdChanged;
	    }

	    public override void OnNetworkSpawn()
	    {
	        UpdateSpriteSheet();
	    }

	    // NTFS: Need to make this work for the arm sprite as well. Not sure why it is not working
	    private void LateUpdate()
	    {
	        if (_spriteSheetLookup == null || _playerPartRenderer.sprite == null)
	            return;

	        ArmorItemSO armorItem = null;
	        if (_armorEquippedId.Value != GameDataRegistry.INVALID_ID)
	        {
	            armorItem = GameDataRegistry.Instance.GetItemDataFromItemId(_armorEquippedId.Value) as ArmorItemSO;
	        }

	        // Overlay armor: overlay tracks base sprite animation
	        if (armorItem != null && armorItem.OverlayArmor && _overlaySpriteRenderer != null)
	        {
	            string baseSpriteName = _playerPartRenderer.sprite.name;
	            if (_spriteSheetLookup.TryGetValue(baseSpriteName, out Sprite overlaySprite))
	            {
	                _overlaySpriteRenderer.sprite = overlaySprite;
	            }
	            else
	            {
	                _overlaySpriteRenderer.sprite = null;
	            }
	        }
	        // Non-overlay: player part renderer uses override sprite directly
	        else if (_spriteSheetLookup != null && _playerPartRenderer.sprite != null)
	        {
	            string baseSpriteName = _playerPartRenderer.sprite.name;
	            if (_spriteSheetLookup.TryGetValue(baseSpriteName, out Sprite overrideSprite))
	            {
	                _playerPartRenderer.sprite = overrideSprite;
	                if (_overlaySpriteRenderer != null)
	                {
	                    _overlaySpriteRenderer.sprite = null;
	                }
	            }
	        }
	    }

	    private void OnArmorUpdated(object sender, ArmorSlotUI.ArmorEquipDataEventArgs e)
	    {
	        if (e.ArmorType != ArmorType) return;

	        _armorEquippedId.Value = e.ArmorItemData != null
	            ? GameDataRegistry.Instance.GetItemIdFromItemData(e.ArmorItemData)
	            : GameDataRegistry.INVALID_ID;
	    }

	    private void OnArmorEquippedIdChanged(ushort previousValue, ushort newValue)
	    {
	        UpdateSpriteSheet();
	    }

	    private void UpdateSpriteSheet()
	    {
	        _spriteSheetLookup = null;

	        if (_armorEquippedId.Value == GameDataRegistry.INVALID_ID)
	        {
	            ClearRenderers();
	            return;
	        }

	        ArmorItemSO armorItem = GameDataRegistry.Instance.GetItemDataFromItemId(_armorEquippedId.Value) as ArmorItemSO;
	        if (armorItem == null) return;

	        Texture2D armorSheet = null;

	        if(_isArmSprite)
	        {
	            armorSheet = armorItem.ArmorSprites.ArmSprites;
	        }
	        else
	        {
	            switch (ArmorType)
	            {
	                case ArmorType.Head:
	                    armorSheet = armorItem.ArmorSprites.HeadSprites;
	                    break;
	                case ArmorType.Chest:
	                    armorSheet = armorItem.ArmorSprites.ChestSprites;
	                    break;
	                case ArmorType.Legs:
	                    armorSheet = armorItem.ArmorSprites.LegsSprites;
	                    break;
	            }
	        }

	        Object[] data = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(armorSheet));
	        _spriteSheetLookup = new Dictionary<string, Sprite>();

	        foreach (Object obj in data)
	        {
	            if (obj is Sprite sprite)
	            {
	                _spriteSheetLookup[sprite.name] = sprite;
	            }
	        }

	        // Immediately apply first sprite
	        ApplyFirstSprite(armorItem);
	    }

	    private void ApplyFirstSprite(ArmorItemSO armorItem)
	    {
	        // On equip, immediately sync overlay or non-overlay with current animation frame if possible
	        if (_spriteSheetLookup == null || _spriteSheetLookup.Count == 0) return;

	        if (armorItem.OverlayArmor && _overlaySpriteRenderer != null && _playerPartRenderer.sprite != null)
	        {
	            string baseSpriteName = _playerPartRenderer.sprite.name;
	            if (_spriteSheetLookup.TryGetValue(baseSpriteName, out Sprite overlaySprite))
	            {
	                _overlaySpriteRenderer.sprite = overlaySprite;
	            }
	        }
	        else
	        {
	            if (_playerPartRenderer.sprite != null)
	            {
	                string baseSpriteName = _playerPartRenderer.sprite.name;
	                if (_spriteSheetLookup.TryGetValue(baseSpriteName, out Sprite overrideSprite))
	                {
	                    _playerPartRenderer.sprite = overrideSprite;
	                }
	            }

	            if (_overlaySpriteRenderer != null) _overlaySpriteRenderer.sprite = null;
	        }
	    }

	    private void ClearRenderers()
	    {
	        if (_overlaySpriteRenderer != null)
	        {
	            _overlaySpriteRenderer.sprite = null;
	        }
	    }

	    private SpriteRenderer GetTargetRenderer()
	    {
	        if (_armorEquippedId.Value == GameDataRegistry.INVALID_ID) return _playerPartRenderer;

	        ArmorItemSO armorItem = GameDataRegistry.Instance.GetItemDataFromItemId(_armorEquippedId.Value) as ArmorItemSO;
	        if (armorItem != null && armorItem.OverlayArmor && _overlaySpriteRenderer != null)
	        {
	            return _overlaySpriteRenderer;
	        }

	        return _playerPartRenderer;
	    }
	}
}