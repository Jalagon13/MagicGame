using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class PlayerSpriteOverrider : NetworkBehaviour
{
    [field: SerializeField] public ArmorType ArmorType { get; private set; }
    [field: SerializeField] public bool UseArmSheet { get; private set; }
    [field: SerializeField] public bool IsAimingArmSprite { get; private set; }
    [field: SerializeField] public Sprite DefaultAimArmSprite { get; private set; } // NTFS: This will cause visual bugs later down the road when loading player with armor on. Fix it later. Just a quick fix for now.

    private NetworkVariable<int> _armorEquippedId = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private HashSet<Sprite> _overrideSheet;
    private SpriteRenderer _playerPartRenderer;
    private Player _thisPlayer;

    private void Awake()
    {
        _playerPartRenderer = GetComponent<SpriteRenderer>();

        if (_thisPlayer == null)
        {
            _thisPlayer = transform.root.GetComponent<Player>();
        }
    }

    private void Start()
    {
        if(_thisPlayer.OwnerClientId == NetworkManager.LocalClientId)
        {
            // PlayerStats.Instance.OnArmorEquipped += OnArmorEquipped;
            // PlayerStats.Instance.OnArmorUnEquipped += OnArmorUnEquipped;
        }

        _armorEquippedId.OnValueChanged += OnArmorEquippedIdChanged;
    }

    public override void OnNetworkSpawn()
    {
        if (GameManager.Instance.GetItemSOFromItemId(_armorEquippedId.Value) is not ArmorItemSO) return;

        UpdateSpriteSheet();
    }

    private void LateUpdate()
    {
        if (_overrideSheet == null) return;

        foreach (var sprite in _overrideSheet)
        {
            string spriteName = sprite.name;
            if (_playerPartRenderer.sprite.name == spriteName)
            {
                _playerPartRenderer.sprite = sprite;
            }
        }
    }

    private void OnArmorEquippedIdChanged(int previousValue, int newValue)
    {
        UpdateSpriteSheet();
    }
    
    private void UpdateSpriteSheet()
    {
        if (_armorEquippedId.Value == -1)
        {
            _overrideSheet = null;
            if (IsAimingArmSprite)
            {
                _playerPartRenderer.sprite = DefaultAimArmSprite;
            }
            return;
        }

        ArmorItemSO armorItem = GameManager.Instance.GetItemSOFromItemId(_armorEquippedId.Value) as ArmorItemSO;
        _overrideSheet = new();

        Texture2D armorSheet = null;

        if (UseArmSheet)
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

        UnityEngine.Object[] data = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(armorSheet));
        if (data != null)
        {
            foreach (UnityEngine.Object obj in data)
            {
                if (obj.GetType() == typeof(Sprite))
                {
                    Sprite sprite = obj as Sprite;
                    _overrideSheet.Add(sprite);
                }
            }
        }
    }

    // private void OnArmorEquipped(object sender, PlayerStats.ArmorChangedEventArgs e)
    // {
    //     if (e.ArmorItem.ArmorType != ArmorType) return;
        
    //     _armorEquippedId.Value = GameManager.Instance.GetItemIdFromItemSO(e.ArmorItem);
    // }

    // private void OnArmorUnEquipped(object sender, PlayerStats.ArmorChangedEventArgs e)
    // {
    //     if (e.ArmorItem.ArmorType != ArmorType) return;
    
    //     _armorEquippedId.Value = -1;
    // }

    public override void OnDestroy()
    {
        if (_thisPlayer.OwnerClientId == NetworkManager.LocalClientId)
        {
            // PlayerStats.Instance.OnArmorEquipped -= OnArmorEquipped;
            // PlayerStats.Instance.OnArmorUnEquipped -= OnArmorUnEquipped;
        }

        _armorEquippedId.OnValueChanged -= OnArmorEquippedIdChanged;
    }
}
